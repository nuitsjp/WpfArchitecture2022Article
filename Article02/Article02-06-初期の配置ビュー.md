# 初期の配置ビュー

予算編で記載したように、購買システムはクライアント・Web API・データベースの三層アーキテクチャを採用します。また購買システムは販売ドメインと製造ドメインに依存するため、それらのノードも意識する必要があります。

購買ドメインの開発が開始しているということは、販売ドメインと製造ドメインの予算も決定されているはずで、購買ドメインの予算編と同等のアーキテクチャは決定されていると想定します。

それらを配置図に起こすとつぎのようになります。

![](/Article02/スライド21.PNG)

購買・販売・製造パッケージがあり、それぞれに論理的なノードと、ノード上に配置されるコンポーネントを記載しています。販売ドメインも製造ドメインも、購買ドメインと同様に、データの永続化にはSQL Serverを利用するものとします。

この時点でもう少し設計を詰められそうなのはつぎの2点でしょうか？

1. 購買APIのアーキテクチャ
2. 販売・製造ドメインとの関係性

## 購買APIのアーキテクチャ

購買APIをRESTでつくるのか？それとも別のものを利用するのか？といったアーキテクチャの選択は、この時点でできることが多いでしょう。逆にいうと、個別のユースケースによって変わるようなものではないとも言えます。

Web API実装の選択肢として、つぎのものを候補として検討します。

1. REST
2. gRPC
3. GraphQL

結論としては今回はgRPCを利用する想定で設計を進めます。理由はいくつかあります。

1. 購買ドメインでは、「発注する」ようなRPC（Remote Procedure Call）のようなスタイルのメッセージがあり、RESTのようなリソース要求スタイルか、RPCスタイルかのどちらか一方に寄せることで設計を簡略化するとした場合、gRPCに寄せたほうが素直な設計に感じる
1. 購買ドメインのWeb APIを直接外部に公開する想定はなく、RESTほどの相互接続性は必要ない（gRPCの相互接続性が低いわけでもないが、RESTほど一般的でもない）
1. 同様にGraphQLほどの柔軟性も必要ない
1. RESTとgRPCでは単純にgRPCの方が軽くて速いことが多い
1. .NETにおけるgRPCでは[MagicOnionというOSS](https://github.com/Cysharp/MagicOnion)を利用することで、C#のインターフェイスベースでの設計・実装が可能で、RPCスタイルのデメリット（エンドポイントが分かりにくい）を解消できる

gRPCの実装にはMagicOnionを利用する想定ということで、モデルに追加します。

![](/Article02/スライド22.PNG)

MagicOnionは、クライアントとサーバーでそれぞれモジュールが異なるので、そのとおり記載しています。

なお初期論理ビューの設計より先にこちらを記載したのは、Web APIを決定しておくことで、初期論理ビューで設計できる部分が増えるためです。

## 販売・製造ドメインとの関係性

購買ドメインでは、販売・製造ドメイン上のオブジェクトを、購買ドメインに同期する必要があります。たとえば購買数を決定するにあたり、販売実績を参照するといった内容を実現するためです。

販売・製造ドメインとの関係性からアーキテクチャを設計する際、つぎの点を考慮する必要があります。

1. 販売・製造ドメインが上流である
2. 販売・製造ドメインとの関係性は腐敗防止層である

販売・製造ドメインは購買ドメインの上流にあたります。

そのため販売・製造コンテキストのオブジェクトを購買ドメイン側で解釈し、購買コンテキストのオブジェクトに変換して取り込みます。それらを販売・製造それぞれの腐敗防止層コンポーネントとして実装することにします。腐敗防止層コンポーネントはひとまず購買データベース上に配置しましょう。

![](/Article02/スライド26.PNG)

腐敗防止層の実装の詳細は、ユースケースビューでユースケースを設計することで詳細化します。ただ腐敗防止層の実装はWPFのアーキテクチャから乖離しすぎるため、「2022年版実践WPF業務アプリケーションのアーキテクチャ」内では取り扱いません。

