# プロセスビューの設計

プロセスビューでは、並行性やパフォーマンス要件で、特別な検討が必要と考えられるアーキテクチャを設計します。ほとんどの場合は、.NET（async/awaitなど）やASP.NET Coreなどがになってくれるため、それらを単純に使うだけなら特別な設計は不要です。

今回のケースではWPFのプロセスの起動のみを設計の対象とします。

というのはWPFアプリケーションをGeneric Host上で利用したいためです。

Generic HostはASP.NET CoreなどでWebアプリケーションやWeb APIをホスティングするための仕組みです。Generic Hostでは多用な機能が提供されていますが、とくに重要なのは .NET公式のDependency Injection(DI)コンテナーが含まれている点にあります。そのためモダンなサードバーティライブラリはGeneric Host前提のものが多数ありますし、今後でてくる魅力的なライブラリーもGeneric Hostを対象にリリースされるでしょう。

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

たとえば画面遷移時に、文字列messageを引数として渡したいとします。その場合、遷移先のViewModelをつぎのように実装します。

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

画面遷移でパラメーターの型不一致が発生したり、デフォルトコンストラクターが前提とならないため、null安全な実装ができる非常に強力な、現状もっとも理想的な画面遷移フレームワーク・・・だと思って私が開発したものです。自画自賛抜きで良くできていると思っているので良かったら使ってみてください。

日本語のドキュメントも十分に用意しています。

- [KAMISHIBAI入門](https://zenn.dev/nuits_jp/books/introduction-to-kamishibai)

## コンテナー初期化とViewの分離

通常WPFプロジェクトを作成すると、アプリケーションのエントリーポイントとXAMLは同じプロジェクト内に作成されます。

しかしこの設計には大きな問題があります。

アプリケーションのエントリーポイントでは、Generic Hostを初期化してアプリケーションをホストします。このときGeneric Hostで利用するDIコンテナーの初期化を行う必要があります。DIコンテナーの初期化を行うという事は、エントリーポイントからはソリューション内のすべてのプロジェクトを参照できる必要があります。

そのため、DIコンテナーの初期化とXAMLを同じプロジェクトに配置すると、XAMLから本来は触る必要のないオブジェクトを操作できるようになってしまいます。これを防ぐためには、エントリーポイントとViewのプロジェクトを分離する必要があります。

また実のところ、Web API側でも同じ問題があります。ASP.NET CoreのDIコンテナーの初期化処理と、VendorRepositoryServiceクラスを同じプロジェクトに配置すると、VendorRepositoryServiceから不要なオブジェクトを参照できてしまいます。

そのためWPF・Web APIともに初期化処理を分離しましょう。これは実装ビューで表現します。

## 実装ビューの更新

更新した実装ビューがつぎの通りです。

![](/Article02/スライド35.PNG)

WPFとWeb APIの初期化処理を実施するProgramクラスを含む、それぞれAdventureWorks.Purchasing.WpfとAdventureWorks.Purchasing.AspNetCoreというプロジェクトを追加しました。

またWPFで重要となるAppクラスをViewのプロジェクトに含めることを合わせて明記しました。テーマやスタイルなどを適用するためには、View側にないと不都合が多いためです。

## 配置ビューの更新

配置ビューも更新しましょう。

![](/Article02/スライド36.PNG)

AdventureWorks.Purchasing.WpfとAdventureWorks.Purchasing.AspNetCoreを追加して依存関係を整理しました。

# さいごに

さて、これでやっとユースケースや非機能要件を設計する準備が整いました。なかなか大変でしたね。ただ、ここまでの設計はシステムに依存しない部分が多いため、次回以降はそのまま流用できる部分が多いです。

後編は、つぎのような内容で進めて、アーキテクチャ設計を完成に導きたいと思います。

1. 代表的なユースケースの実現
2. 非機能要件の実現
3. 開発者ビューの設計

という訳で、ここまでお付き合いありがとうございました！後編で再びお会いしましょう！