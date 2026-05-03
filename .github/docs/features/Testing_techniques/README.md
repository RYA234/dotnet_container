# テスト技法デモ

## 概要

同値分割・境界値分析・デシジョンテーブル・状態遷移テストを実例付きで学べるデモ機能。
各技法の概念説明・入力例・期待結果をインタラクティブに確認できる。

## 技法一覧

| # | 技法 | 画面パス | フォルダ |
|---|------|---------|--------|
| 01 | 同値分割 | /Demo/TestingTechniques/EquivalencePartitioning | [01-equivalence-partitioning](01-equivalence-partitioning/) |
| 02 | 境界値分析 | /Demo/TestingTechniques/BoundaryValue | [02-boundary-value](02-boundary-value/) |
| 03 | デシジョンテーブル | /Demo/TestingTechniques/DecisionTable | [03-decision-table](03-decision-table/) |
| 04 | 状態遷移テスト | /Demo/TestingTechniques/StateTransition | [04-state-transition](04-state-transition/) |

## 共通シナリオ

| 技法 | シナリオ |
|------|---------|
| 同値分割・境界値分析 | 年齢によるユーザー区分判定（子供/一般/シニア） |
| デシジョンテーブル | 会員ランク×購入金額×クーポンで割引率を決定 |
| 状態遷移テスト | 注文ステータスの遷移（注文受付→完了/キャンセル） |

## ドキュメント構成（各技法共通）

| ドキュメント | 内容 |
|------------|------|
| external-design.md | 画面設計・API仕様 |
| internal-design.md | クラス設計・処理フロー |
| requirements.md | 機能要件・教育目的 |
| test-cases.md | テストケース一覧 |
