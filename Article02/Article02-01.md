2022年版実践WPF業務アプリケーションのアーキテクチャ【設計編　前編】 ～ ドメイン駆動設計＆Clean Architectureとともに ～

# リード文（400字以内）

先日、「[2022年版実践WPF業務アプリケーションのアーキテクチャ【見積編】～ドメイン駆動設計＆Clean Architectureとともに～](https://codezine.jp/article/detail/16953)」という記事を公開させていただきました。こちらの記事では、見積時に考慮が必要なアプリケーションアーキテクチャの着眼点や、その具体的な検討内容について記載しました。

今回はいよいよ「設計編　機能要件の実現」ということで、見積が承認され、開発が開始して以降のフェーズに入ります。

開発が開始して以降、アーキテクチャに対して影響を与える要件として「機能要件」と「非機能要件」の2つがあります。これら2つの要件からはじまって、アーキテクチャを設計していくひとつの方法をお伝えしたいと思います。


# 前提条件

本稿はWPFアプリケーションのアーキテクチャ設計について記載したものです。[見積編](https://codezine.jp/article/detail/16953)を前提に記載していますので、まだ未読であれば見積編からお読みください。

本稿にはサーバーサイドの設計も一部含まれていますが、見積編にも記載した通り、サーバーサイドについてはWPFアプリケーションを設計する上で、必要最低限の範囲に限定しています。サーバーサイドの実現方式は、オンプレ環境なのかクラウド環境なのか？といった容易などで大きく変わってきます。そしてWPFアプリケーションから見た場合には本質的な問題ではありません。サーバーサイドまで厳密に記載すると話が発散し過ぎてしまい、WPFアプリケーションのアーキテクチャにフォーカスすることが難しくなるため、あくまで参考程度にご覧ください。

また本稿ではAdventureWorks社の業務のうち、発注業務システムのアーキテクチャとなります。特定業務は発注業務に限定されますが、認証などの複数の業務にまたがったアーキテクチャの実現についても言及します。

本稿は以下の環境を前提に記載しています。  

* Visual Studio 2022 Version 17.4.0
* Docker Desktop 4.14.0
* Docker version 20.10.20
* SQL Server 2022-latest(on Docker)
* [ComponentOne for WPF Edition 2022v2](https://www.grapecity.co.jp/developer/componentone/wpf)
* [SPREAD for WPF 4.0J](https://www.grapecity.co.jp/developer/spread-wpf)
* Test Assistant Pro 1.123
* .NET 6.0.11

本稿のサンプルは .NET 6で構築しますが、.NET Framework 4.6.2以上（.NET Standard 2.0水準以上）であれば同様のアーキテクチャで実現可能です。ただし一部利用しているパッケージのバージョンを当てなおす必要があるかもしれません。

# 想定読者

次の技術要素の基本をある程度理解していることを想定しています。

* C#  
* WPF  
* Docker
* SQL Server

これらの基本的な解説は、本稿では割愛しますが、知らないと理解できないという訳でもありません。

また下記の2つも概要は理解できていることが好ましいです。

* Clean Architecture
* ドメイン駆動設計（DDD）

Clean Architectureについては、筆者のブログである「[世界一わかりやすいClean Architecture](https://www.nuits.jp/entry/easiest-clean-architecture-2019-09)」をあわせて読んでいただけると、本稿のアーキテクチャの設計意図が伝わりやすいかと思います。

ドメイン駆動設計の適用範囲については、本文内でも、つど解説いたします。