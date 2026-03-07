# 設定管理 単体テスト仕様書

## 文書情報
- **作成日**: 2026-03-07
- **最終更新**: 2026-03-07
- **バージョン**: 1.0
- **ステータス**: ドラフト
- **関連設計書**: [設定管理設計](configuration.md)

---

## 1. テスト対象

### 1.1 テスト対象コンポーネント

| コンポーネント | 説明 |
|-------------|------|
| 設定値の読み込み | `appsettings.json` / 環境別ファイル / 環境変数の優先順位 |
| 接続文字列の取得 | `GetConnectionString` による接続文字列の取得と欠損時の例外 |
| 起動時バリデーション | 必須設定値が欠損している場合の起動時エラー検出 |
| 環境別設定の切り替え | 環境名（Development / Test / Production）に応じた設定上書き |

---

## 2. テスト計画

### 2.1 テスト方針

1. **優先順位の正確性**: 環境変数 > 環境別ファイル > 基本ファイルの順で設定値が上書きされること
2. **欠損検出の確実性**: 必須設定値が存在しない場合に起動時に例外がスローされること
3. **環境別切り替えの正確性**: 環境名に応じた設定ファイルが正しく読み込まれること
4. **シークレット混入防止**: テストコードに接続文字列・APIキー等をハードコードしないこと

---

### 2.2 テストレベル

| テストレベル | 対象 | 目的 |
|------------|------|------|
| 単体テスト | Service クラスの設定値取得ロジック | 設定値が正しく取得・利用されることの確認 |
| 統合テスト | `WebApplicationFactory` + `appsettings.Test.json` | 実際の設定読み込みフローの確認 |

---

### 2.3 テストカバレッジ目標

| カテゴリ | 目標カバレッジ | 備考 |
|---------|--------------|------|
| 接続文字列取得ロジック | 100% | 正常・欠損の両パターンをカバー |
| 起動時バリデーション | 100% | 欠損時の例外スローを確認 |
| 環境別設定切り替え | 90% | Development / Test / Production の各環境をカバー |

---

## 3. 設定値読み込みのテストケース

### 3.1 優先順位

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-CF-001 | 環境変数が appsettings.json より優先される | appsettings.json に `DemoDatabase=file.db`、環境変数に `ConnectionStrings__DemoDatabase=env.db` を設定 | 設定値を取得 | 取得値が `env.db`（環境変数が優先） | 高 |
| TC-CF-002 | appsettings.{Environment}.json が appsettings.json より優先される | appsettings.json に `Default=Information`、appsettings.Development.json に `Default=Debug` を設定 | Development 環境で取得 | 取得値が `Debug` | 高 |
| TC-CF-003 | 環境別ファイルが存在しない場合、appsettings.json の値を使用する | appsettings.json のみ存在、環境別ファイルなし | 設定値を取得 | appsettings.json の値が返る | 中 |

---

### 3.2 接続文字列の取得

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-CF-004 | 接続文字列が存在する場合、値を返す | `ConnectionStrings:DemoDatabase` が設定済み | `GetConnectionString("DemoDatabase")` を呼び出し | 設定値の文字列が返る | 高 |
| TC-CF-005 | 接続文字列が存在しない場合、例外をスローする | `ConnectionStrings:DemoDatabase` が未設定 | `GetConnectionString("DemoDatabase")` を呼び出し | `InvalidOperationException` がスローされる | 高 |
| TC-CF-006 | 接続文字列が空文字の場合、例外をスローする | `ConnectionStrings:DemoDatabase=""` | `GetConnectionString("DemoDatabase")` を呼び出し | `InvalidOperationException` がスローされる | 高 |

---

## 4. 起動時バリデーションのテストケース

### 4.1 必須設定値の検出

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-CF-007 | 必須接続文字列が欠損している場合、起動時に例外をスローする | `ConnectionStrings:DemoDatabase` が未設定 | アプリケーション起動 | `InvalidOperationException` がスローされ起動が中断される | 高 |
| TC-CF-008 | 必須設定値がすべて存在する場合、正常に起動する | 必要な設定値がすべて設定済み | アプリケーション起動 | 例外なく起動が完了する | 高 |

---

## 5. 環境別設定のテストケース

### 5.1 環境名による切り替え

| No | テストケース名 | 前提条件 | 入力データ | 期待結果 | 優先度 |
|----|--------------|---------|----------|---------|-------|
| TC-CF-009 | Test 環境でインメモリDB接続文字列が使用される | `appsettings.Test.json` に `Data Source=:memory:` を設定 | Test 環境で接続文字列を取得 | `Data Source=:memory:` が返る | 高 |
| TC-CF-010 | Development 環境で開発用DB接続文字列が使用される | `appsettings.Development.json` に `Data Source=demo-dev.db` を設定 | Development 環境で接続文字列を取得 | `Data Source=demo-dev.db` が返る | 中 |
| TC-CF-011 | 存在しないキーに対して null が返る | 設定ファイルにキーが存在しない | `_configuration["NonExistent:Key"]` を取得 | `null` が返る（例外はスローされない） | 中 |

---

## 6. テスト実装ガイドライン

### 6.1 テストツール推奨

| 用途 | 推奨ツール |
|------|----------|
| テストフレームワーク | xUnit |
| アサーション | FluentAssertions |
| 設定のモック | `IConfiguration` の Mock または `ConfigurationBuilder` でインメモリ設定を構築 |

---

### 6.2 接続文字列の取り扱い規約

接続文字列・APIキー等のシークレットは `IConfiguration` 経由でのみアクセスすること。以下は**絶対禁止**。

| 禁止事項 | 理由 |
|---------|------|
| View / HTML テンプレートへの接続文字列の埋め込み | クライアントへの情報漏洩リスク |
| ソースコードへのハードコード | Git リポジトリへのシークレット混入 |
| ログへの接続文字列の出力 | ログ経由の情報漏洩リスク |
| Service / Controller コンストラクタ以外での文字列リテラル指定 | `IConfiguration` DI による管理の徹底 |

**テストでの確認方法**:
- `IConfiguration` が Mock として注入されていること（= 直接ハードコードされていないことの間接証明）
- テストコード自体にも接続文字列をハードコードしない（`appsettings.Test.json` または `ConfigurationBuilder` で管理）

### 6.3 テストデータ管理

1. **接続文字列はテストコードにハードコードしない**: `appsettings.Test.json` または `ConfigurationBuilder` で管理する
2. **実際のファイルに依存しない単体テスト**: `ConfigurationBuilder` でインメモリ設定を構築してテストする
3. **統合テストはインメモリDB**: `appsettings.Test.json` の `Data Source=:memory:` を使用する

---

## 7. テスト実行計画

### 7.1 実行順序

1. 設定値読み込みのテスト（TC-CF-001 〜 TC-CF-003）
2. 接続文字列取得のテスト（TC-CF-004 〜 TC-CF-006）
3. 起動時バリデーションのテスト（TC-CF-007 〜 TC-CF-008）
4. 環境別設定のテスト（TC-CF-009 〜 TC-CF-011）

---

### 7.2 テスト環境

| 項目 | 要件 |
|------|------|
| データベース | インメモリ DB または Mock |
| 設定ファイル | `appsettings.Test.json` を使用 |
| 外部依存 | なし（設定管理は外部依存なし） |

---

## 8. 参考

- [設定管理設計](configuration.md)
- [テスト設計](testing.md)

### Microsoft Learn 公式リファレンス

| 内容 | URL |
|------|-----|
| ASP.NET Core の構成 | https://learn.microsoft.com/ja-jp/aspnet/core/fundamentals/configuration/ |
| ASP.NET Core の統合テスト | https://learn.microsoft.com/ja-jp/aspnet/core/test/integration-tests |
| WebApplicationFactory | https://learn.microsoft.com/ja-jp/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1 |
| xUnit でのテスト（.NET） | https://learn.microsoft.com/ja-jp/dotnet/core/testing/unit-testing-with-dotnet-test |
