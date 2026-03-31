# Claude AI Development Instructions

**コードの生成・修正・Issue作成など、いかなる作業を開始する前に、必ず最初に `.github/copilot-instructions.md` を Read ツールで読んでください。読まずに作業を開始することは禁止です。**

読んだ上で、記載されている内容を必ず遵守してください。

## コード生成時の必須手順

コードを生成・修正する前に以下を必ず実行してください。

1. `.github/copilot-instructions.md` を読む
2. 以下の規約に従ってコードを生成する

## 必須: XMLドキュメントコメント

すべての `public` クラス・メソッドに以下を記載してください。

```csharp
/// <summary>
/// [概要]
/// </summary>
/// <remarks>
/// <para><strong>設計書:</strong> .github/docs/features/[機能名]/internal-design.md</para>
/// <para><strong>責務:</strong> [責務の説明]</para>
/// <para><strong>アルゴリズム:</strong></para>
/// <list type="number">
/// <item><description>[手順1]</description></item>
/// <item><description>[手順2]</description></item>
/// </list>
/// </remarks>
```

DBアクセスがある場合は必ずSQL文を `<code>` で記載してください。

## 必須: 定数・マジックナンバー

数値や文字列のハードコードは禁止です。必ず名前付き定数にしてください。

```csharp
// NG
const int batchSize = 500;

// OK
/// <summary>SQLiteの1回のINSERTで扱える最大行数</summary>
private const int BatchSize = 500;
```

## 必須: 設計書との同期

- コード修正後は必ず `.github/docs/features/[機能名]/` の設計書を更新する
- 技術判断が必要な変更は `.github/docs/adr/` にADRを作成する
