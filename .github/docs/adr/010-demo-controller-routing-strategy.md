# ADR-010: DemoControllerのルーティング戦略

## ステータス
採用済み

## 日付
2026-03-17

## コンテキスト
Demo機能をFeatureごとのフォルダ構成にリファクタリングする際、Controllerの分割方法を検討した。

- 現状：`DemoController` 1クラスに全機能（N+1/FullScan/SelectStar/LikeSearch/Validation/Logging/ErrorHandling/Security/DataStructures/DatabaseConnection）が混在
- 機能追加のたびにコンストラクタの引数が増え続ける
- URL `/dotnet/Demo/Performance` 等を変更したくない

## 決定
`[Route("Demo")]` を各Controllerクラスに付与し、機能ごとに独立したControllerクラスに分割する。

```csharp
[Route("Demo")]
public class NPlusController : Controller
{
    [Route("Performance")]
    public IActionResult Performance() { ... }
}
```

## 理由
### 採用理由
- URLを変更せずに（`/dotnet/Demo/Performance` のまま）独立Controllerに分割できる
- コンストラクタが機能単位でシンプルになる（必要なサービスのみDI）
- GitHubでファイルを開いた時に1機能が1ファイルに収まり見通しが良い
- 機能追加時の影響範囲が明確になる

### 代替案との比較
| 案 | URL変更 | 独立性 | 見通し |
|---|---|---|---|
| partial class（旧） | なし | 低（コンストラクタ共有） | 分散して追いにくい |
| **[Route]統一（採用）** | **なし** | **高** | **機能=1ファイル** |
| Controller名でURL変更 | あり | 高 | 高 |

## 結果
### ポジティブな影響
- 各ControllerのDI依存が最小化される
- 機能ごとにファイルが独立するため、コードレビューが容易になる

### ネガティブな影響
- `[Route("Demo")]` の重複がController数分発生する
- Controllerクラス名とURLが一致しない（`NPlusController` でも URL は `/Demo/...`）

## 関連 ADR
- なし
