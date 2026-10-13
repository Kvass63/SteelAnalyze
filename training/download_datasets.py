"""Шаг 1. Скачивает три открытых набора снимков дефектов стали и раскладывает их
по папкам raw_datasets/<набор>/<класс>/.

Запуск:  python download_datasets.py
Нужен только Python, дополнительных библиотек не требуется.
"""
import shutil
import urllib.request
import zipfile
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parent
DOWNLOADS = ROOT / "_downloads"   # сюда скачиваются архивы
RAW = ROOT / "raw_datasets"       # сюда раскладываются снимки

IMAGE_EXTENSIONS = {".jpg", ".jpeg", ".png", ".bmp"}

# Набор -> откуда скачивается архив
SOURCES = {
    # NEU-DET, Северо-Восточный университет (Китай): 6 классов по 300 снимков
    "NEU": "https://www.kaggle.com/api/v1/datasets/download/kaustubhdikshit/neu-surface-defect-database",
    # GC10-DET: 10 классов (архив около 920 МБ)
    "GC10": "https://www.kaggle.com/api/v1/datasets/download/zhangyunsheng/defects-class-and-location",
    # X-SDD, репозиторий авторов набора: 7 классов, 1360 снимков
    "X-SDD": "https://github.com/Fighter20092392/X-SDD-A-New-benchmark/archive/refs/heads/main.zip",
}


def download(name: str, url: str) -> Path:
    archive = DOWNLOADS / f"{name}.zip"
    if archive.exists():
        print(f"[{name}] архив уже скачан: {archive.name}")
        return archive

    print(f"[{name}] скачивание...")
    DOWNLOADS.mkdir(exist_ok=True)
    partial = archive.with_suffix(".part")  # чтобы оборванная загрузка не выглядела готовым архивом
    request = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(request) as response, open(partial, "wb") as file:
        shutil.copyfileobj(response, file, length=1024 * 1024)
    partial.rename(archive)
    print(f"[{name}] скачано {archive.stat().st_size / 1024 / 1024:.0f} МБ")
    return archive


def extract(name: str, archive: Path) -> None:
    target = RAW / name
    if target.exists():
        print(f"[{name}] папка {target.name} уже есть, распаковка пропущена")
        return

    counts = Counter()
    with zipfile.ZipFile(archive) as zf:
        for entry in zf.infolist():
            path = Path(entry.filename)
            if entry.is_dir() or path.suffix.lower() not in IMAGE_EXTENSIONS:
                continue

            # Класс снимка — это имя папки, в которой он лежит внутри архива
            class_name = path.parent.name
            destination = target / class_name / path.name
            if destination.exists():
                print(f"[{name}] повтор имени файла, пропущен: {entry.filename}")
                continue

            destination.parent.mkdir(parents=True, exist_ok=True)
            with zf.open(entry) as source, open(destination, "wb") as file:
                shutil.copyfileobj(source, file)
            counts[class_name] += 1

    print(f"[{name}] распаковано снимков: {sum(counts.values())}")
    for class_name in sorted(counts):
        print(f"    {counts[class_name]:5d}  {class_name}")


def main() -> None:
    for name, url in SOURCES.items():
        extract(name, download(name, url))
    print("\nГотово. Следующий шаг: python prepare_dataset.py")


if __name__ == "__main__":
    main()
