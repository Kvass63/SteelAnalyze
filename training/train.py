"""Шаг 3. Обучает сеть и сохраняет её в файл model.onnx.

EfficientNet-B0, триплетная функция потерь, подбор трудных отрицательных примеров.

Запуск:
    python train.py --smoke    быстрая проверка, что всё установлено и видеокарта работает
    python train.py            полное обучение
"""
import argparse
import json
import random
import sys
import time
from datetime import datetime
from pathlib import Path

import cv2
import numpy as np
import torch
import torch.nn.functional as F

from export_onnx import export_onnx
from model import IMG_SIZE, EfficientNetEmbedding

# Настройки обучения
MARGIN = 0.5                 # на сколько «чужой» снимок должен быть дальше «своего»
BATCH_SIZE = 32              # троек снимков в одном шаге обучения
EPOCHS = 30
HARD_MINING_SAMPLES = 128    # среди скольких случайных снимков ищется трудный отрицательный пример
METRICS_FREQ = 5             # раз во сколько эпох измеряется качество
LEARNING_RATE = 1e-4
SEED = 42

ROOT = Path(__file__).resolve().parent
DATASET_DIR = ROOT / "dataset" / "images"
OUTPUT_DIR = ROOT / "output"
IMAGE_EXTENSIONS = {".jpg", ".jpeg", ".png", ".bmp"}


def load_split(split: str, classes: list[str], limit: int | None) -> tuple[torch.Tensor, torch.Tensor]:
    """Читает снимки выборки. Предобработка: RGB, сжатие до 224x224."""
    images, labels = [], []
    for class_index, class_name in enumerate(classes):
        paths = sorted(p for p in (DATASET_DIR / split / class_name).iterdir() if p.suffix.lower() in IMAGE_EXTENSIONS)
        for path in paths[:limit]:
            # imdecode вместо imread: imread не открывает файл, если в пути к нему есть русские буквы
            image = cv2.imdecode(np.fromfile(path, dtype=np.uint8), cv2.IMREAD_COLOR)
            if image is None:
                print(f"  [пропуск] файл не читается как изображение: {path}")
                continue
            image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
            images.append(cv2.resize(image, (IMG_SIZE, IMG_SIZE)))
            labels.append(class_index)

    # Снимки хранятся целыми числами 0..255 (так они занимают вчетверо меньше памяти), порядок осей: снимок, канал, высота, ширина
    return torch.from_numpy(np.stack(images)).permute(0, 3, 1, 2).contiguous(), torch.tensor(labels)


def to_input(images: torch.Tensor, device: torch.device) -> torch.Tensor:
    """Готовит снимки ко входу сети: перенос на устройство и значения от 0 до 1."""
    return images.to(device).float() / 255.0


@torch.no_grad()
def embed(model: torch.nn.Module, images: torch.Tensor, device: torch.device) -> torch.Tensor:
    """Считает эмбеддинги снимков в режиме работы (без обучения)."""
    model.eval()
    chunks = [model(to_input(images[i:i + 64], device)).cpu() for i in range(0, len(images), 64)]
    return torch.cat(chunks)


def sample_triplets(model, images, labels, class_indices, device):
    """Собирает один батч троек «якорь — свой — чужой» и возвращает номера снимков.

    Чужой снимок берётся не случайно, а самый похожий на якорь из пула (трудный отрицательный пример):
    на таких тройках сеть учится различать именно то, что она пока путает.
    """
    pool = torch.randint(0, len(images), (min(HARD_MINING_SAMPLES, len(images)),))
    pool_embeddings = embed(model, images[pool], device)
    pool_labels = labels[pool]

    anchors, positives = [], []
    for _ in range(BATCH_SIZE):
        # Класс выбирается равновероятно, поэтому редкие классы попадают в обучение так же часто, как частые
        members = class_indices[torch.randint(0, len(class_indices), (1,)).item()]
        anchor = members[torch.randint(0, len(members), (1,))].item()
        others = members[members != anchor]
        anchors.append(anchor)
        positives.append(others[torch.randint(0, len(others), (1,))].item())
    anchors, positives = torch.tensor(anchors), torch.tensor(positives)

    similarity = embed(model, images[anchors], device) @ pool_embeddings.T
    same_class = labels[anchors].unsqueeze(1) == pool_labels.unsqueeze(0)
    similarity[same_class] = -float("inf")  # снимки своего класса не могут быть отрицательным примером
    negatives = pool[similarity.argmax(dim=1)]

    # Если в пуле оказались снимки только класса якоря, чужой снимок берётся случайно
    for row in torch.where(same_class.all(dim=1))[0]:
        candidates = torch.where(labels != labels[anchors[row]])[0]
        negatives[row] = candidates[torch.randint(0, len(candidates), (1,))]

    return anchors, positives, negatives


def triplet_loss(anchor: torch.Tensor, positive: torch.Tensor, negative: torch.Tensor) -> torch.Tensor:
    """Штраф тем больше, чем ближе к якорю чужой снимок по сравнению со своим."""
    positive_distance = torch.sum((anchor - positive) ** 2, dim=1)
    negative_distance = torch.sum((anchor - negative) ** 2, dim=1)
    return F.relu(positive_distance - negative_distance + MARGIN).mean()


def train_epoch(model, optimizer, images, labels, class_indices, device, steps: int) -> float:
    total_loss = 0.0
    for step in range(steps):
        anchors, positives, negatives = sample_triplets(model, images, labels, class_indices, device)

        model.train()
        loss = triplet_loss(
            model(to_input(images[anchors], device)),
            model(to_input(images[positives], device)),
            model(to_input(images[negatives], device)),
        )

        optimizer.zero_grad()
        loss.backward()
        torch.nn.utils.clip_grad_norm_(model.parameters(), max_norm=1.0)
        optimizer.step()
        total_loss += loss.item()

        if (step + 1) % max(1, steps // 4) == 0:
            print(f"  шаг {step + 1}/{steps}, потери: {total_loss / (step + 1):.4f}")

    return total_loss / steps


def vote(neighbor_labels: list[int]) -> int:
    """Класс по голосованию ближайших эталонов; при равенстве голосов — тот, чей эталон стоит выше.
    То же правило, что в Web API (DefectClassifier.cs)."""
    return max(set(neighbor_labels), key=lambda c: (neighbor_labels.count(c), -neighbor_labels.index(c)))


def evaluate(query_embeddings, query_labels, gallery_embeddings, gallery_labels, classes: list[str]) -> dict:
    """Для каждого проверяемого снимка находит ближайшие эталоны в галерее и считает показатели качества."""
    similarity = query_embeddings @ gallery_embeddings.T
    nearest = similarity.topk(min(10, len(gallery_labels)), dim=1).indices
    nearest_labels = gallery_labels[nearest]
    hits = nearest_labels == query_labels.unsqueeze(1)

    predicted = torch.tensor([vote(row[:5].tolist()) for row in nearest_labels])
    confusion = torch.zeros(len(classes), len(classes), dtype=torch.long)
    for true_label, predicted_label in zip(query_labels.tolist(), predicted.tolist()):
        confusion[true_label, predicted_label] += 1

    return {
        # Recall@K: доля снимков, у которых среди K ближайших эталонов есть эталон верного класса
        "recall_at_1": hits[:, :1].any(dim=1).float().mean().item(),
        "recall_at_5": hits[:, :5].any(dim=1).float().mean().item(),
        "recall_at_10": hits.any(dim=1).float().mean().item(),
        # Доля снимков, класс которых верно определён голосованием 5 ближайших эталонов (так работает Web API)
        "vote_accuracy": (predicted == query_labels).float().mean().item(),
        "per_class_accuracy": {
            name: (confusion[i, i] / max(1, confusion[i].sum())).item() for i, name in enumerate(classes)
        },
        "confusion_matrix": confusion.tolist(),
    }


def main() -> None:
    parser = argparse.ArgumentParser(description="Обучение сети распознавания дефектов стали")
    parser.add_argument("--smoke", action="store_true", help="быстрая проверка на малой части данных")
    parser.add_argument("--epochs", type=int, default=EPOCHS, help=f"число эпох (по умолчанию {EPOCHS})")
    args = parser.parse_args()

    random.seed(SEED)
    np.random.seed(SEED)
    torch.manual_seed(SEED)

    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    if device.type == "cuda":
        memory = torch.cuda.get_device_properties(0).total_memory / 1024 ** 3
        print(f"Устройство: видеокарта {torch.cuda.get_device_name(0)} ({memory:.0f} ГБ)")
        torch.backends.cudnn.benchmark = True
    else:
        print("Устройство: процессор. Видеокарта не найдена — полное обучение займёт много часов.")

    if not (DATASET_DIR / "train").is_dir():
        sys.exit(f"Не найден датасет {DATASET_DIR}. Сначала запустите download_datasets.py и prepare_dataset.py")

    # В режиме проверки берётся по 12 снимков класса и делается 2 шага обучения
    limit = 12 if args.smoke else None
    epochs = 1 if args.smoke else args.epochs
    output_dir = OUTPUT_DIR / "smoke" if args.smoke else OUTPUT_DIR
    onnx_path = output_dir / "model.onnx" if args.smoke else ROOT / "model.onnx"
    metrics_path = output_dir / "metrics.json" if args.smoke else ROOT / "metrics.json"
    weights_path = output_dir / "best.pth"
    output_dir.mkdir(parents=True, exist_ok=True)

    print("\n[1/4] Загрузка датасета...")
    classes = sorted(p.name for p in (DATASET_DIR / "train").iterdir() if p.is_dir())
    train_images, train_labels = load_split("train", classes, limit)
    val_images, val_labels = load_split("val", classes, limit)
    test_images, test_labels = load_split("test", classes, limit)
    print(f"Классов: {len(classes)}. Снимков: train {len(train_images)}, val {len(val_images)}, test {len(test_images)}")

    class_indices = [torch.where(train_labels == i)[0] for i in range(len(classes))]
    steps = 2 if args.smoke else len(train_images) // BATCH_SIZE

    print("\n[2/4] Обучение...")
    model = EfficientNetEmbedding(pretrained=True).to(device)
    optimizer = torch.optim.Adam(model.parameters(), lr=LEARNING_RATE)
    scheduler = torch.optim.lr_scheduler.CosineAnnealingLR(optimizer, T_max=epochs)

    history = []
    best_recall = -1.0
    started = time.time()
    for epoch in range(1, epochs + 1):
        loss = train_epoch(model, optimizer, train_images, train_labels, class_indices, device, steps)
        scheduler.step()

        elapsed = time.time() - started
        remaining = elapsed / epoch * (epochs - epoch)
        print(f"Эпоха {epoch}/{epochs}: потери {loss:.4f}, прошло {elapsed / 60:.1f} мин, осталось около {remaining / 60:.0f} мин")

        if epoch % METRICS_FREQ == 0 or epoch == epochs:
            # Проверочные снимки сравниваются с обучающими: так выбирается лучшая эпоха
            metrics = evaluate(embed(model, val_images, device), val_labels,
                               embed(model, train_images, device), train_labels, classes)
            print(f"  проверочная выборка: Recall@1 {metrics['recall_at_1']:.4f}, Recall@5 {metrics['recall_at_5']:.4f}")
            history.append({"epoch": epoch, "loss": loss, "recall_at_1": metrics["recall_at_1"], "recall_at_5": metrics["recall_at_5"]})

            if metrics["recall_at_1"] > best_recall:
                best_recall = metrics["recall_at_1"]
                torch.save(model.state_dict(), weights_path)
                print(f"  лучший результат, веса сохранены: {weights_path.name}")

    print("\n[3/4] Итоговая проверка на тестовой выборке (эти снимки сеть при обучении не видела)...")
    model.load_state_dict(torch.load(weights_path, map_location=device))
    train_embeddings = embed(model, train_images, device)
    test_embeddings = embed(model, test_images, device)

    # Галерея — обучающая и проверочная выборки вместе, как база эталонов в Web API
    gallery_embeddings = torch.cat([train_embeddings, embed(model, val_images, device)])
    gallery_labels = torch.cat([train_labels, val_labels])
    final = evaluate(test_embeddings, test_labels, gallery_embeddings, gallery_labels, classes)

    print(f"Recall@1:  {final['recall_at_1']:.4f}")
    print(f"Recall@5:  {final['recall_at_5']:.4f}")
    print(f"Recall@10: {final['recall_at_10']:.4f}")
    print(f"Точность голосования 5 ближайших (как в Web API): {final['vote_accuracy']:.4f}")
    print("\nТочность по классам:")
    for name, value in final["per_class_accuracy"].items():
        print(f"  {name:16s} {value:.4f}")

    print("\n[4/4] Сохранение модели в ONNX...")
    export_onnx(weights_path, onnx_path)

    with open(metrics_path, "w", encoding="utf-8") as file:
        json.dump({
            "trained_at": datetime.now().isoformat(timespec="seconds"),
            "smoke_run": args.smoke,
            "device": torch.cuda.get_device_name(0) if device.type == "cuda" else "cpu",
            "torch_version": torch.__version__,
            "epochs": epochs,
            "training_minutes": round((time.time() - started) / 60, 1),
            "classes": classes,
            "images": {"train": len(train_images), "val": len(val_images), "test": len(test_images)},
            "history": history,
            "test": final,
        }, file, ensure_ascii=False, indent=2)

    print(f"\nГотово. Модель: {onnx_path}, показатели: {metrics_path}")
    if args.smoke:
        print("Это была проверка на малой части данных. Полное обучение: python train.py")


if __name__ == "__main__":
    main()
