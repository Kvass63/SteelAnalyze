"""Архитектура сети: EfficientNet-B0 и надстройка, выдающая эмбеддинг."""
import os

# Отключает длинное предупреждение Windows о символических ссылках при скачивании весов; на работу не влияет
os.environ.setdefault("HF_HUB_DISABLE_SYMLINKS_WARNING", "1")

import timm
import torch
import torch.nn as nn
import torch.nn.functional as F

IMG_SIZE = 224        # сторона входного снимка в пикселях
EMBEDDING_DIM = 128   # длина эмбеддинга


class EfficientNetEmbedding(nn.Module):
    """Превращает снимок в вектор из 128 чисел единичной длины.

    pretrained=True — взять готовые веса EfficientNet-B0, обученные на ImageNet (нужно для обучения).
    pretrained=False — пустая сеть, в которую затем загружаются наши обученные веса.
    """

    def __init__(self, model_name: str = "efficientnet_b0", embedding_dim: int = EMBEDDING_DIM, pretrained: bool = True):
        super().__init__()

        # num_classes=0: без слоя классификации, сеть отдаёт 1280 признаков снимка
        self.backbone = timm.create_model(model_name, pretrained=pretrained, num_classes=0)
        feature_dim = self.backbone.num_features

        # Надстройка сжимает признаки до эмбеддинга: 1280 -> 512 -> 256 -> 128
        self.embedding_head = nn.Sequential(
            nn.Linear(feature_dim, 512),
            nn.BatchNorm1d(512),
            nn.ReLU(),
            nn.Dropout(0.3),
            nn.Linear(512, 256),
            nn.BatchNorm1d(256),
            nn.ReLU(),
            nn.Dropout(0.3),
            nn.Linear(256, embedding_dim),
        )

    def forward(self, x: torch.Tensor) -> torch.Tensor:
        x = self.backbone(x)
        x = self.embedding_head(x)
        # Вектор приводится к единичной длине: тогда скалярное произведение двух эмбеддингов — это их сходство
        return F.normalize(x, p=2, dim=1)
