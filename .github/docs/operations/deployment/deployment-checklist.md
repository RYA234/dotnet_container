# デプロイチェックリスト

## 概要
デプロイ作業を安全かつ確実に実施するためのチェックリストです。デプロイ前、デプロイ中、デプロイ後の各フェーズで確認すべき項目をまとめています。

---

## デプロイ前チェックリスト

### 1. コード品質確認

#### 1.1 テスト実施
- [ ] すべてのユニットテストがパスしている
- [ ] 統合テストがパスしている
- [ ] E2Eテスト（Playwright）がパスしている
- [ ] テストカバレッジが基準（80%以上）を満たしている

```bash
# ローカルでテスト実行
dotnet test

# カバレッジ確認
dotnet test /p:CollectCoverage=true
```

#### 1.2 コードレビュー
- [ ] Pull Requestがレビュー済み
- [ ] レビューコメントがすべて解決済み
- [ ] 最低1名の承認を得ている
- [ ] マージコンフリクトがない

#### 1.3 ビルド確認
- [ ] ローカルでのビルドが成功
- [ ] CI/CDパイプラインのビルドが成功
- [ ] Dockerイメージのビルドが成功
- [ ] コンパイラ警告がない

```bash
# ローカルビルド
dotnet build --configuration Release

# Dockerビルド
docker build -t dotnet-app:test .
```

#### 1.4 静的解析
- [ ] コードフォーマットが統一されている
- [ ] Linterのエラーがない
- [ ] セキュリティスキャンでCritical/Highの問題がない

---

### 2. 変更内容の確認

#### 2.1 変更範囲
- [ ] 変更内容を理解している
- [ ] 影響範囲を把握している
- [ ] 破壊的変更がないことを確認（または対策済み）
- [ ] データベーススキーマ変更がないことを確認（または対策済み）

#### 2.2 依存関係
- [ ] 依存ライブラリのアップデートを確認
- [ ] 脆弱性のある依存関係がない
- [ ] 互換性の問題がない

```bash
# 依存関係の脆弱性チェック
dotnet list package --vulnerable
```

#### 2.3 設定変更
- [ ] 環境変数の変更内容を確認
- [ ] Secrets Managerの更新が必要な場合は対応済み
- [ ] タスク定義の変更が必要な場合は準備済み

---

### 3. 環境確認

#### 3.1 本番環境の状態
- [ ] 本番環境が正常稼働中
- [ ] 現在のCPU/メモリ使用率が正常範囲内
- [ ] 最近のエラーログがない
- [ ] 予定されたメンテナンスがない

```bash
# ECSサービス状態確認
aws ecs describe-services --cluster app-cluster --services dotnet-service --region ap-northeast-1

# ヘルスチェック
curl https://rya234.com/dotnet/healthz
```

#### 3.2 リソース確認
- [ ] ECRにプッシュするための十分な容量がある
- [ ] ECSタスクを起動するための十分なリソースがある
- [ ] AWSのサービス制限に抵触しない

#### 3.3 バックアップ
- [ ] データベースのバックアップが最新
- [ ] 必要に応じて手動バックアップを実施
- [ ] ロールバック手順を確認済み

---

### 4. デプロイ計画

#### 4.1 タイミング
- [ ] デプロイ実施日時が決定
- [ ] 業務時間外のデプロイ（推奨）
- [ ] 重要なイベント前のデプロイを避ける
- [ ] 金曜日の夜のデプロイを避ける

#### 4.2 コミュニケーション
- [ ] 関係者にデプロイ計画を通知済み
- [ ] デプロイ中の連絡手段を確保
- [ ] 緊急連絡先を確認

#### 4.3 準備
- [ ] デプロイ手順書を確認
- [ ] ロールバック手順を確認
- [ ] トラブルシューティングガイドを準備
- [ ] 監視ダッシュボードを開く

---

## デプロイ中チェックリスト

### 5. デプロイ実施

#### 5.1 デプロイ開始
- [ ] デプロイ開始時刻を記録
- [ ] デプロイ方法を選択（自動/手動）
- [ ] デプロイコマンドを実行

```bash
# 自動デプロイの場合
git push origin main

# 手動デプロイの場合
aws ecs update-service --cluster app-cluster --service dotnet-service --force-new-deployment --region ap-northeast-1
```

#### 5.2 デプロイ監視
- [ ] GitHub Actionsのログを監視（自動デプロイの場合）
- [ ] ECSサービスの状態を監視
- [ ] タスクの起動状況を監視
- [ ] CloudWatch Logsを監視

```bash
# リアルタイムログ監視
aws logs tail /ecs/dotnet-app --follow --region ap-northeast-1

# サービス状態監視
watch -n 10 "aws ecs describe-services --cluster app-cluster --services dotnet-service --region ap-northeast-1 --query 'services[0].[runningCount,desiredCount]'"
```

#### 5.3 エラー対応
- [ ] エラーが発生した場合は即座に対応
- [ ] 重大なエラーの場合はロールバックを検討
- [ ] エラー内容と対応を記録

---

## デプロイ後チェックリスト

### 6. 動作確認

#### 6.1 ヘルスチェック
- [ ] ヘルスチェックエンドポイントが200を返す
- [ ] すべてのタスクが正常に起動
- [ ] デザイアドカウント = ランニングカウント

```bash
# ヘルスチェック
curl -i https://rya234.com/dotnet/healthz

# ECS状態確認
aws ecs describe-services --cluster app-cluster --services dotnet-service --region ap-northeast-1 --query 'services[0].[runningCount,desiredCount,status]'
```

#### 6.2 機能確認
- [ ] トップページが表示される
- [ ] 主要機能が動作する
- [ ] APIエンドポイントが正常に応答
- [ ] 認証・認可が正常に機能

```bash
# トップページ確認
curl -I https://rya234.com/dotnet

# 各エンドポイントの確認
curl https://rya234.com/dotnet/api/...
```

#### 6.3 ログ確認
- [ ] エラーログが出ていない
- [ ] 警告ログの内容を確認
- [ ] アプリケーションログが正常に出力されている

```bash
# 最新10分間のログ確認
aws logs tail /ecs/dotnet-app --since 10m --region ap-northeast-1 --format short

# エラーログ検索
aws logs filter-log-events --log-group-name /ecs/dotnet-app --filter-pattern "ERROR" --region ap-northeast-1
```

---

### 7. パフォーマンス確認

#### 7.1 リソース使用率
- [ ] CPU使用率が正常範囲内（<80%）
- [ ] メモリ使用率が正常範囲内（<80%）
- [ ] ネットワークトラフィックが正常

```bash
# CloudWatchメトリクス確認
aws cloudwatch get-metric-statistics \
  --namespace AWS/ECS \
  --metric-name CPUUtilization \
  --dimensions Name=ClusterName,Value=app-cluster Name=ServiceName,Value=dotnet-service \
  --start-time $(date -u -d '10 minutes ago' +%Y-%m-%dT%H:%M:%S) \
  --end-time $(date -u +%Y-%m-%dT%H:%M:%S) \
  --period 300 \
  --statistics Average \
  --region ap-northeast-1
```

#### 7.2 レスポンスタイム
- [ ] APIレスポンスタイムが許容範囲内（<2秒）
- [ ] ページロード時間が正常
- [ ] データベースクエリが遅延していない

```bash
# レスポンスタイム測定
time curl -s https://rya234.com/dotnet/healthz > /dev/null
```

---

### 8. 監視・アラート

#### 8.1 監視設定
- [ ] CloudWatch Logsが正常に記録されている
- [ ] メトリクスが正常に収集されている
- [ ] アラートが正常に動作している

#### 8.2 しばらく様子を見る
- [ ] デプロイ後30分間は異常がないか監視
- [ ] エラー率の上昇がないか確認
- [ ] ユーザーからの問い合わせがないか確認

---

### 9. 完了処理

#### 9.1 記録
- [ ] デプロイ完了時刻を記録
- [ ] デプロイしたバージョン（タスク定義リビジョン、イメージタグ）を記録
- [ ] 発生した問題と対応を記録

#### 9.2 通知
- [ ] 関係者にデプロイ完了を通知
- [ ] 問題があった場合は詳細を共有
- [ ] 次回のデプロイ予定を共有（ある場合）

```
件名: [完了] 本番環境デプロイ完了

本番環境へのデプロイが完了しました。

- デプロイ日時: 2025-12-17 14:00-14:15 JST
- デプロイ内容: [変更内容の概要]
- デプロイバージョン: dotnet-task:15
- 状況: 正常稼働中

動作確認結果:
✓ ヘルスチェック正常
✓ 主要機能動作確認済み
✓ エラーログなし

今後30分間は監視を継続します。
```

#### 9.3 ドキュメント更新
- [ ] リリースノートを更新（該当する場合）
- [ ] 運用ドキュメントを更新（変更があった場合）
- [ ] トラブルシューティングガイドを更新（新しい問題があった場合）

---

### 10. クリーンアップ

#### 10.1 リソースクリーンアップ
- [ ] 古いECRイメージを削除（ストレージコスト削減）
- [ ] 不要なタスク定義を確認
- [ ] ローカルのDockerイメージを削除

```bash
# 古いECRイメージの確認（最新10個以外）
aws ecr describe-images \
  --repository-name dotnet-app \
  --region ap-northeast-1 \
  --query 'sort_by(imageDetails,& imagePushedAt)[:-10].[imageDigest,imagePushedAt]' \
  --output table

# ローカルイメージのクリーンアップ
docker image prune -a
```

#### 10.2 GitHub整理
- [ ] デプロイ済みのブランチを削除
- [ ] 関連するIssueをクローズ
- [ ] Pull Requestをクローズ

```bash
# マージ済みのブランチを削除
git branch -d feature/xxx
git push origin --delete feature/xxx

# Issueをクローズ
gh issue close <issue-number> --comment "デプロイ完了により解決"
```

---

## ロールバックチェックリスト

### 11. ロールバックが必要な場合

#### 11.1 ロールバック判断
- [ ] ロールバックが必要な理由を明確化
- [ ] ロールバック先のバージョンを決定
- [ ] 関係者に通知

#### 11.2 ロールバック実施
- [ ] ロールバック手順を確認
- [ ] ロールバック実行

```bash
# 前のバージョンにロールバック
aws ecs update-service --cluster app-cluster --service dotnet-service --task-definition dotnet-task:14 --force-new-deployment --region ap-northeast-1
```

#### 11.3 ロールバック後確認
- [ ] サービスが正常稼働
- [ ] ヘルスチェックが成功
- [ ] エラーログがない
- [ ] 関係者に完了を通知

詳細は [rollback.md](rollback.md) を参照してください。

---

## 定期的なレビュー

### チェックリストの改善
- [ ] 四半期ごとにチェックリストをレビュー
- [ ] 発生した問題を反映
- [ ] 不要な項目を削除
- [ ] 新しいベストプラクティスを追加

---

## 関連ドキュメント

- [自動デプロイ手順](automated-deployment.md)
- [手動デプロイ手順](manual-deployment.md)
- [ロールバック手順](rollback.md)
- [トラブルシューティング](../troubleshooting/common-issues.md)

---

**最終更新日**: 2025-12-17
