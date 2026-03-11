# SELECT * 無駄遣いデモ

## 概要

`SELECT *` による全カラム取得と、必要なカラムのみ取得（`SELECT Id, Name, Email`）を比較し、
転送データ量・AWS転送料の差を数値で体感できるデモ。

## ドキュメント一覧

| ドキュメント | 内容 |
|------------|------|
| [要件定義書](requirements.md) | 機能要件・非機能要件・FinObs観点 |
| [外部設計書](external-design.md) | API仕様・画面設計・データ型定義 |
| [内部設計書](internal-design.md) | クラス設計・処理フロー・DDL |
| [テストケース](test-cases.md) | ユニットテスト・手動テスト一覧 |

## デモの流れ

```
Step 1: セットアップ（1万件 × 35KB/件 = 350MB相当のデータ生成）
Step 2: SELECT * 実行（全カラム取得 → 350MB転送）
Step 3: 必要カラムのみ実行（Id, Name, Email → 500KB転送）
→ サイズ差・AWS転送料差を比較
```

## 教育ポイント

- **SELECT * は楽だが、不要なデータを大量に転送する**
- 1万件で 350MB vs 500KB ＝ **700倍の差**
- 月100万リクエストなら AWS転送料 **$3,500 vs $5**
- 画面に表示しないカラムは取得しないことを習慣化する

## 関連 Issue

- [Issue #16](https://github.com/RYA234/dotnet_container/issues/16)
