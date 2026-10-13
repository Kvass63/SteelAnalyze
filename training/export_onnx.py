"""Шаг 4. Переводит обученные веса (.pth) в файл ONNX, который запускает Web API на C#.

train.py делает это сам в конце обучения. Отдельный запуск нужен, только если
требуется заново получить ONNX из уже сохранённых весов:

    python export_onnx.py output/best.pth model.onnx
"""
import sys
from pathlib import Path

import numpy as np
import onnxruntime as ort
import torch

from model import IMG_SIZE, EfficientNetEmbedding

INPUT_NAME = "image"          # вход: снимки N x 3 x 224 x 224, RGB, значения от 0 до 1
OUTPUT_NAME = "embedding"     # выход: эмбеддинги N x 128


def export_onnx(weights_path: Path, onnx_path: Path) -> None:
    model = EfficientNetEmbedding(pretrained=False)
    model.load_state_dict(torch.load(weights_path, map_location="cpu"))
    model.eval()  # режим работы, а не обучения: Dropout отключается, BatchNorm использует накопленную статистику

    example = torch.rand(1, 3, IMG_SIZE, IMG_SIZE)
    torch.onnx.export(
        model,
        (example,),
        str(onnx_path),
        input_names=[INPUT_NAME],
        output_names=[OUTPUT_NAME],
        dynamic_axes={INPUT_NAME: {0: "batch"}, OUTPUT_NAME: {0: "batch"}},  # снимков за раз может быть сколько угодно
        opset_version=17,
        dynamo=False,
    )

    # Проверка: ONNX-модель должна выдавать те же числа, что и исходная
    batch = torch.rand(4, 3, IMG_SIZE, IMG_SIZE)
    with torch.no_grad():
        expected = model(batch).numpy()
    session = ort.InferenceSession(str(onnx_path), providers=["CPUExecutionProvider"])
    actual = session.run([OUTPUT_NAME], {INPUT_NAME: batch.numpy()})[0]

    difference = float(np.abs(expected - actual).max())
    print(f"ONNX сохранён: {onnx_path} ({onnx_path.stat().st_size / 1024 / 1024:.1f} МБ)")
    print(f"Наибольшее расхождение с исходной моделью: {difference:.2e}")
    if difference > 1e-3:
        sys.exit("Ошибка: ONNX-модель выдаёт другие эмбеддинги, чем исходная.")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        sys.exit("Запуск: python export_onnx.py <веса .pth> <файл .onnx>")
    export_onnx(Path(sys.argv[1]), Path(sys.argv[2]))
