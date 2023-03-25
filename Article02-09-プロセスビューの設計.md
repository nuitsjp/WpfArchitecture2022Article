# プロセスビューの設計

並行性やパフォーマンス要件で特別な検討が必要と考えられるアーキテクチャを設計します。ほとんどの場合は、.NET（async/awaitなど）やASP.NET Coreなどがになってくれるため、それらを単純に使うだけなら特別な設計は不要です。

今回のケースではWPFのプロセスの起動のみを設計の対象とします。

というのはWPFアプリケーションをGeneric Host上で利用したいためです。

Generic HostはASP.NET CoreなどでWebアプリケーションやWeb APIをホスティングするための仕組みです。Generic Hostでは多用な機能が提供されていますが、とくに重要なのは.NET公式のDependency Injection(DI)コンテナーが含まれている点にあります。そのためモダンなサードバーティライブラリはGeneric Host前提のものが多数ありますし、今後でてくる魅力的なライブラリーもGeneric Hostを対象にリリースされるでしょう。

WPFの標準的な実装ではGeneric Hostにホストされていませんが、できないわけではありません。

## フレームワークの選定

WPFをGeneric Host上にホストするライブラリーは、つぎの2つがNuGet上に公開されています。

1. [Dapplo.Microsoft.Extensions.Hosting.Wpf](https://github.com/dapplo/Dapplo.Microsoft.Extensions.Hosting)
2. [Wpf.Extensions.Hosting](https://github.com/nuitsjp/Wpf.Extensions.Hosting)

今回は後者のWpf.Extensions.Hostingを利用します。

これはWpf.Extensions.Hostingをベースとしている画面遷移フレームワーク「[Kamishibai](https://github.com/nuitsjp/Kamishibai)」を利用したいためです。

Kamishibaiは、つぎのような特徴をもつWPF用の画面遷移フレームワークです。


- Generic Hostのサポート
- MVVMパターンを適用したViewModel起点の画面遷移
- 型安全性の保証された画面遷移時パラメーター
- 画面遷移にともなう一貫性あるイベント通知
- nullableを最大限活用するためのサポート

たとえば画面遷移時に文字列messageを引数として渡したいとします。その場合、遷移先のViewModelをつぎのように実装します。

```cs
[Navigate]
public class FirstViewModel
{
    public FirstViewModel(string message)
    {
        Message = message;
    }

    public string Message { get; }
}
```

すると専用の画面遷移メソッドが自動生成され、つぎのように呼び出すことができます。

```cs
await _presentationService.NavigateToFirstAsync("Hello, KAMISHIBAI!");
```

画面遷移でパラメーターの型不一致が発生したり、デフォルトコンストラクタが前提とならないため、null安全な実装ができる非常に強力な、現状もっとも理想的な画面遷移フレームワーク・・・だと思って私が開発したものです。自画自賛抜きで良くできていると思っているので良かったら使ってみてください。

日本語のドキュメントも十分に容易しています。

- [KAMISHIBAI入門](https://zenn.dev/nuits_jp/books/introduction-to-kamishibai)

## コンテナー初期化とViewの分離

通常WPFプロジェクトを作成すると、アプリケーションのエントリーポイントとXAMLは同じプロジェクト内に作成されます。

しかしこの設計には大きな問題があります。

アプリケーションのエントリーポイントでは、Generic Hostを初期化してアプリケーションをホストします。このときGeneric Hostで利用するDIコンテナーの初期化を行う必要があります。DIコンテナーの初期化を行うという事は、エントリーポイントからはソリューション内のすべてのプロジェクトを参照できる必要があります。

論理ビューを更新してみましょう。

![](/Article02/スライド34.PNG)

Programクラスがアプリケーションのエントリーポイントになります。WPFプロジェクトのcsprojでEnableDefaultApplicationDefinitionをfalseに設定し、Programクラスを自動生成しないように変更し、明示的に作成します。

上図からわかるようにProgramはすべてのレイヤーに対して依存関係が発生してしまいます。

しかしAppやWindowなどからたとえばPurchasingDatabaseを直接触れるようにしておきたくありません。

これを防ぐためには、エントリーポイントとViewのプロジェクトを分離する必要があります。

それは実装ビューで表現します。

## 実装ビューと配置ビューの更新

要素が増えてきたので、レイアウトも論理ビューと近い配置に変更しました。

![](/Article02/スライド35.PNG)

Programを配置したAdventureWorks.Purchasing.Appコンポーネントがすべてのコンポーネントに依存していることが見て取れます。

配置ビューも更新しましょう。

![](/Article02/スライド36.PNG)

購買アプリや購買サービスという曖昧なコンポーネントを削除して、それぞれAdventureWorks.Purchasing.AppとAdventureWorks.Purchasing.Serviceに置き換えて依存関係も整理しました。

クライアントとWeb APIノードの間の依存が一時的に失われましたが、このあとユースケースの実装を設計していくなかですぐに登場してきますので、ここではこのままにしておきましょう。

# さいごに

さて、これでやっとユースケースや非機能要件の実装を設計する準備が整いました。

後編は、つぎのような内容で進めて、アーキテクチャ設計を完成に導きたいと思います。

1. 代表的なユースケースの実現
2. 非機能要件の実現
3. 開発者ビューの設計

という訳で、ここまでお付き合いありがとうございました！後編で再びお会いしましょう！