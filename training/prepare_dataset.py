"""Шаг 2. Собирает из трёх исходных наборов (raw_datasets/) единый датасет из 10 классов (dataset/).

Перенос модуля prepare_dataset.py из приложения А ВКР. Отличия от оригинала:
- имена исходных папок приведены к тем, что реально лежат в скачанных архивах;
- вместо pandas и tqdm используется стандартная библиотека, ставить ничего не нужно.

Запуск:  python prepare_dataset.py
"""
import csv
import random
import shutil
import sys
from collections import Counter, defaultdict
from pathlib import Path

random.seed(42)  # одно и то же разбиение при каждом запуске

ROOT = Path(__file__).resolve().parent
RAW_DIR = ROOT / "raw_datasets"
OUTPUT_DIR = ROOT / "dataset"
IMG_DIR = OUTPUT_DIR / "images"

IMAGE_EXTENSIONS = {".jpg", ".jpeg", ".png", ".bmp"}

# Доли выборок: обучающая, проверочная, тестовая
SPLITS = {"train": 0.7, "val": 0.15, "test": 0.15}

# Схема сопоставления классов (таблица 7 ВКР): набор -> исходная папка -> единый класс
CLASS_MAPPING = {
    "GC10": {
        "crease": "crack",
        "crescent_gap": "crescent_gap",
        "inclusion": "inclusion",
        "oil_spot": "stain",
        "punching_hole": "hole",
        "rolled_pit": "pitted_surface",
        "silk_spot": "stain",
        "water_spot": "stain",
        "welding_line": "weld_line",
        # Папка "waist folding" (поясная складка) не используется. В коде ВКР она записана
        # с опечаткой ("waits folding"), поэтому скрипт её пропускал — так и получилось 10 классов, а не 11
    },
    "NEU": {
        "crazing": "crack",
        "inclusion": "inclusion",
        "patches": "stain",
        "pitted_surface": "pitted_surface",
        "rolled-in_scale": "oxide_scale",
        "scratches": "scratch",
    },
    # В тексте ВКР этот набор назван X-DDD; настоящее название — X-SDD
    "X-SDD": {
        "finishing roll printing": "roll_mark",
        "iron sheet ash": "stain",
        "oxide scale of plate system": "oxide_scale",
        "oxide scale of temperature system": "oxide_scale",
        "red iron": "stain",
        "slag inclusion": "inclusion",
        "surface scratch": "scratch",
    },
}


# Сколько снимков каждого класса было в датасете ВКР (всего 5225).
# Числа получены из рисунка 29 ВКР: сумма строки матрицы ошибок — это число снимков класса.
VKR_CLASS_COUNTS = {
    "crack": 329,
    "crescent_gap": 226,
    "hole": 218,
    "inclusion": 685,
    "oxide_scale": 566,
    "pitted_surface": 330,
    "roll_mark": 203,
    "scratch": 434,
    "stain": 1961,
    "weld_line": 273,
}


def collect_dataset() -> list[dict]:
    """Обходит исходные наборы и для каждого снимка запоминает путь, единый класс и источник."""
    dataset = []

    for source, mapping in CLASS_MAPPING.items():
        source_dir = RAW_DIR / source
        if not source_dir.is_dir():
            sys.exit(f"Не найдена папка {source_dir}. Сначала запустите: python download_datasets.py")

        print(f"\nЧтение {source}...")
        for class_dir in sorted(p for p in source_dir.iterdir() if p.is_dir()):
            unified = mapping.get(class_dir.name.strip().lower())
            if unified is None:
                print(f"  [пропуск] класс не входит в схему: {class_dir.name}")
                continue

            images = sorted(p for p in class_dir.rglob("*") if p.suffix.lower() in IMAGE_EXTENSIONS)
            print(f"  {len(images):5d}  {class_dir.name} -> {unified}")
            for path in images:
                dataset.append({"path": path, "class": unified, "source": source})

    print(f"\nВсего снимков: {len(dataset)}")
    return dataset


def split_dataset(data: list[dict]) -> dict[str, list[dict]]:
    """Делит снимки на выборки отдельно внутри каждого класса, чтобы доли классов сохранились."""
    random.shuffle(data)

    by_class = defaultdict(list)
    for item in data:
        by_class[item["class"]].append(item)

    splits = {name: [] for name in SPLITS}
    for items in by_class.values():
        train_end = int(len(items) * SPLITS["train"])
        val_end = train_end + int(len(items) * SPLITS["val"])

        splits["train"].extend(items[:train_end])
        splits["val"].extend(items[train_end:val_end])
        splits["test"].extend(items[val_end:])

    return splits


def build_split(split_name: str, data: list[dict], metadata: list[dict]) -> None:
    """Копирует снимки выборки в dataset/images/<выборка>/<класс>/ под новыми именами."""
    print(f"Копирование выборки {split_name}: {len(data)} снимков...")
    counter = Counter()

    for item in data:
        counter[item["class"]] += 1
        # В имени файла остаётся источник, например NEU_crack_000001.jpg
        source_tag = item["source"].replace("-", "")
        new_name = f"{source_tag}_{item['class']}_{counter[item['class']]:06d}{item['path'].suffix.lower()}"

        destination = IMG_DIR / split_name / item["class"] / new_name
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(item["path"], destination)

        metadata.append({"filename": new_name, "class": item["class"], "source": item["source"], "split": split_name})


def write_csv(name: str, header: list[str], rows: list[list]) -> None:
    with open(OUTPUT_DIR / name, "w", newline="", encoding="utf-8") as file:
        writer = csv.writer(file)
        writer.writerow(header)
        writer.writerows(rows)


def save_tables(metadata: list[dict]) -> None:
    """Сохраняет описание датасета: список файлов, схему классов и статистику."""
    write_csv("metadata.csv", ["filename", "class", "source", "split"],
              [[m["filename"], m["class"], m["source"], m["split"]] for m in metadata])

    write_csv("class_mapping.csv", ["source", "raw_class", "unified_class"],
              [[source, raw, unified] for source, mapping in CLASS_MAPPING.items() for raw, unified in mapping.items()])

    by_class = defaultdict(Counter)
    for m in metadata:
        by_class[m["class"]][m["split"]] += 1
    rows = [[cls, *(by_class[cls][s] for s in SPLITS), sum(by_class[cls].values())] for cls in sorted(by_class)]
    write_csv("class_statistics.csv", ["class", *SPLITS, "total"], rows)

    by_source = Counter(m["source"] for m in metadata)
    write_csv("source_statistics.csv", ["source", "count"], [[s, by_source[s]] for s in sorted(by_source)])

    print("\nКласс             train   val  test  всего  в ВКР")
    for cls, train, val, test, total in rows:
        expected = VKR_CLASS_COUNTS.get(cls, 0)
        note = "совпадает" if total == expected else f"расхождение {total - expected:+d}"
        print(f"{cls:16s} {train:6d} {val:5d} {test:5d} {total:6d} {expected:6d}  {note}")
    print(f"{'всего':16s} {'':18s} {len(metadata):6d} {sum(VKR_CLASS_COUNTS.values()):6d}")
    print("\nИсточник:")
    for source in sorted(by_source):
        print(f"  {source:6s} {by_source[source]}")


def main() -> None:
    if OUTPUT_DIR.exists():
        sys.exit(f"Папка {OUTPUT_DIR} уже существует. Удалите её и запустите скрипт снова.")

    data = collect_dataset()
    splits = split_dataset(data)

    metadata = []
    for split_name, items in splits.items():
        build_split(split_name, items, metadata)
    save_tables(metadata)

    print(f"\nДатасет готов: {OUTPUT_DIR}")
    print("  ".join(f"{name}: {len(items)}" for name, items in splits.items()))


if __name__ == "__main__":
    main()
