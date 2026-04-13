# ローカル開発環境セットアップ

## 文書情報
- **作成日**: 2026-04-06
- **バージョン**: 1.0

---

## 1. 前提条件

以下がインストール済みであること。

| ツール | バージョン | 用途 |
|--------|-----------|------|
| .NET SDK | 10.0以上 | アプリ実行・テスト |
| Docker Desktop | 最新 | コンテナ起動・TestContainers |
| Git | 最新 | バージョン管理 |

以下のコマンドでインストール済みか確認する。

```bash
dotnet --version
docker -v
git -v
```

---

## 2. リポジトリのクローン

```bash
git clone https://github.com/RYA234/dotnet_container.git
cd dotnet_container
```

---

## 3. 環境変数の設定

`.env.example` をコピーして `.env` を作成する。

```bash
cp .env.example .env
```

`.env` を編集してSupabase設定を追加する。

```ini
Supabase__Url=https://your-project.supabase.co
Supabase__AnonKey=your-anon-key-here
```

---

## 4. アプリの起動

### Docker Composeで起動（推奨）

```bash
docker compose up -d --build
```

ブラウザ: http://localhost:5000/dotnet

停止:

```bash
docker compose down
```

### .NET SDKで起動

```bash
dotnet run --project src/BlazorApp/BlazorApp.csproj
```

---

## 5. テストの実行

### 単体テスト・統合テスト

```bash
dotnet test BlazorApp.Tests/
```

### E2Eテスト（Playwright）

アプリが起動していることを確認してから実行する。

```bash
# E2Eテスト実行
dotnet test BlazorApp.E2ETests/BlazorApp.E2ETests.csproj --filter "FullyQualifiedName~ValidationDemo"

# 全件のE2Eテスト実行　時間がかかる....
dotnet test BlazorApp.E2ETests/
```

テスト実行後の出力先：

```
BlazorApp.E2ETests/bin/Debug/net10.0/
├── screenshots/    ← スクリーンショット
└── videos/         ← 動画
```

---

## 6. Supabase接続確認（未検証）

アプリ起動後、以下のエンドポイントで接続を確認できる。

```
http://localhost:5000/dotnet/supabase/test
```

---

## 7. 参考

- [E2Eテスト内部設計書](../common/e2e-test-internal.md)
- [デプロイ手順](deployment/manual-deployment.md)
