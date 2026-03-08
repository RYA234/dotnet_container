# ADR-003: テストプロジェクトを本番コードから分離する

## 文書情報
- **作成日**: 2026-03-07
- **バージョン**: 1.0
- **ステータス**: 採用

---

## 背景

テストコードをどこに配置するかについて、以下の2つの選択肢を検討した。

**選択肢A: Feature 内 `Tests/` フォルダに配置**

```
src/BlazorApp/
└── Features/
    └── Demo/
        ├── DemoController.cs
        ├── DemoService.cs
        └── Tests/
            └── DemoServiceTests.cs
```

**選択肢B: 別プロジェクトとして分離**

```
src/
└── BlazorApp/
tests/
├── BlazorApp.UnitTests/
└── BlazorApp.E2ETests/
```

---

## 決定

**選択肢B（別プロジェクト分離）を採用する。**

テストコードは本番コードと別プロジェクトに配置し、さらに単体テストと E2E テストを別プロジェクトに分ける。

```
src/
└── BlazorApp/          ← 本番コード

tests/
├── BlazorApp.UnitTests/   ← 単体テスト・統合テスト
└── BlazorApp.E2ETests/    ← E2E テスト
```

---

## 理由

### 本番コードからの分離

1. **ビルド成果物にテストコードが混入しない**: 本番ビルドにテスト用パッケージ（xUnit・Moq 等）が含まれることを防げる
2. **依存関係の分離**: テスト用パッケージは本番プロジェクトの依存から除外できる
3. **責務の明確化**: 本番コードとテストコードのフォルダ構造が明確に分離されている

### 単体テストと E2E テストの分離

1. **CI 実行速度の最適化**: PR 時は単体テスト（`BlazorApp.UnitTests`）のみ実行し、フィードバックを高速化できる
2. **E2E テストの独立実行**: E2E テストはマージ後や夜間バッチなど別タイミングで実行できる
3. **実行環境の違いを管理しやすい**: 単体テストはインメモリDB、E2E テストは実際の HTTP サーバーを必要とするなど、必要な実行環境が異なる

---

## 却下した選択肢

### Feature 内 `Tests/` フォルダへの配置（選択肢A）

- **却下理由**: 本番プロジェクトにテスト用パッケージへの依存が生じる。本番ビルド成果物にテストコードが含まれるリスクがある。
- **利点として認識していた点**: 対象クラスとテストコードが近く、ナビゲーションしやすい。Feature-based アーキテクチャとの親和性が高い。

> **補足**: テストプロジェクト内でも Feature ごとのフォルダ構成を維持することで、ナビゲーションのしやすさは確保できる。

---

## テストプロジェクト内のフォルダ構成

Feature-based の構成は `BlazorApp.UnitTests` 内でも維持する。

```
tests/
├── BlazorApp.UnitTests/
│   ├── Features/
│   │   └── Demo/
│   │       ├── NPlusOneServiceTests.cs
│   │       └── DemoControllerTests.cs
│   └── Shared/
│       ├── ExceptionTests.cs
│       └── ExceptionHandlingMiddlewareTests.cs
│
└── BlazorApp.E2ETests/
    └── Features/
        └── Demo/
            └── DemoApiTests.cs
```

---

## 影響

- `testing.md` のフォルダ配置規約を本構成に合わせて更新する
- CI ワークフローは以下の方針で設定する
  - PR 時: `BlazorApp.UnitTests` のみ実行
  - マージ後（または夜間）: `BlazorApp.E2ETests` を含む全テストを実行

---

## 参考

- [テスト設計](../testing.md)
- [Feature-based アーキテクチャ ADR](001-use-sqlite-for-education.md)
