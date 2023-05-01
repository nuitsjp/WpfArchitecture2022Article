## ロギング配置ビューの設計

それでは実装ビューで導出したコンポーネントをノード上に配置しましょう。とくに難しい要素もなく、これまでの繰り返しになってしまうため、さらっと説明します。

実装ビューのコンポーネントをノード上に配置したものが以下の図です。

![](Article03/スライド36.PNG)

初期化処理時のシーケンスをたどってみましょう。

![](Article03/スライド37.PNG)

問題ありませんね。ロギング処理時のパスも、ViewModelからのほぼ同様なので説明は割愛します。

1点補足があるとすると、購買API側のログ出力は、SerilogのSqlServerSinkを直接使うことです。そのため、特別な仕組みは必要ありません。






