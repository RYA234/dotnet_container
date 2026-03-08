# ADR-004: シークレットへのアクセスは IConfiguration 経由に限定する

## 文書情報
- **作成日**: 2026-03-07
- **バージョン**: 1.0
- **ステータス**: 採用

---

## 背景

接続文字列・APIキー・認証情報などのシークレットは、取り扱いを誤ると以下の経路で漏洩する。

| 漏洩経路 | 具体例 |
|---------|-------|
| クライアントへの露出 | View / HTML テンプレートにハードコードされた接続文字列がレスポンスに含まれる |
| Git リポジトリへの混入 | ソースコードに直書きされたシークレットがコミット履歴に残る |
| ログ経由の漏洩 | 接続文字列をそのままログ出力し、ログファイル経由で流出する |
| テストコードへの混入 | テストコードにハードコードされたシークレットが CI ログや成果物に露出する |

これらのリスクを防ぐために、シークレットへのアクセス方法をアーキテクチャレベルで統一する。

---

## 決定

**シークレットへのアクセスは `IConfiguration` の DI（依存性注入）経由のみ許可する。アクセスできる層は Repository（データアクセス層）に限定する。**

### 許可する方法

```
Repository（データアクセス層）のみ
  └── IConfiguration → GetConnectionString / GetSection / GetValue
```

- `IConfiguration` は Repository クラスのコンストラクタインジェクションで受け取り、そこから取得する
- Service / Controller は Repository インターフェース経由でデータを取得し、接続文字列には直接アクセスしない
- 設定値の実体は `appsettings.json` / `appsettings.{Environment}.json` / 環境変数で管理する

### 禁止する方法

| 禁止事項 | 理由 |
|---------|------|
| View / HTML テンプレートへの接続文字列の埋め込み | クライアントへの情報漏洩リスク |
| ソースコードへのハードコード | Git リポジトリへのシークレット混入 |
| ログへの接続文字列の出力 | ログ経由の情報漏洩リスク |
| テストコードへのハードコード | CI ログ・成果物への漏洩リスク |
| Service / Controller での `IConfiguration` の直接利用 | DB接続の責務はデータアクセス層（Repository）に限定する |
| `IConfiguration` 以外の静的プロパティ・グローバル変数での管理 | アクセス経路の分散による管理不能 |

---

## 理由

1. **アクセス経路の一本化**: `IConfiguration` を唯一のアクセス経路にすることで、設定値の取得箇所が明確になりレビューしやすくなる
2. **環境ごとの差し替えが容易**: DI 経由のため、テスト時に Mock や `appsettings.Test.json` で安全な値に差し替えられる
3. **シークレットの Git 混入防止**: `appsettings.json` に書かれる値はデフォルト値のみ。シークレットは環境変数または User Secrets で管理し、Git にコミットされない
4. **View からの分離**: View / HTML はプレゼンテーション層であり、DB 接続情報へのアクセス権を持つべきでない

---

## 却下した選択肢

### 静的クラスでの一元管理

```
static class AppSettings {
    public static string ConnectionString = "Data Source=demo.db";
}
```

- **却下理由**: ソースコードにシークレットが混入する。テストでの差し替えが困難。

### 環境変数を直接 `Environment.GetEnvironmentVariable` で取得

- **却下理由**: アクセス経路が分散し、どこで何の設定値を使っているか追跡が困難になる。`IConfiguration` に統一することで ASP.NET Core の設定プロバイダー（ファイル / 環境変数 / User Secrets）の優先順位制御を活用できる。

---

## 影響

- **コードレビューのチェック項目**: 以下を必ず確認する
  - View / HTML に接続文字列・APIキーが含まれていないこと
  - ソースコードに接続文字列をハードコードしていないこと
  - ログ出力に接続文字列が含まれていないこと
  - テストコードに接続文字列をハードコードしていないこと
- **テスト**: `IConfiguration` が Mock として注入されていることを確認することで、直接アクセスしていないことを間接的に担保する
- **静的解析の検討**: Roslyn Analyzer 等でハードコードを検出するルールの導入を将来的に検討する

---

## 参考

- [設定管理設計](../internal-design/configuration.md)
- [設定管理テスト仕様書](../internal-design/configuration-test.md)
- ASP.NET Core 公式ドキュメント: Safe storage of app secrets in development
