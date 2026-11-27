# AnimeShelf

AnimeShelf é um catálogo local de animes para Windows. Ele encontra episódios em pastas escolhidas pelo usuário, consulta metadados públicos do AniList e acompanha o progresso de reprodução no próprio computador.

O projeto está em desenvolvimento. A versão atual do código é a **0.11.1**.

## Funcionalidades

- Varredura de uma ou mais pastas e reprodução de episódios no aplicativo padrão do Windows.
- Busca e associação de animes pelo AniList, com correção manual por título ou ID.
- Exibição em detalhes, lista compacta ou grade de capas, com filtros e progresso local.
- Edição pessoal do título, da sinopse e da capa; essas alterações permanecem após novas varreduras.
- Capas salvas localmente ou carregadas pela internet, conforme a configuração escolhida.
- Interface em português, inglês e espanhol, com temas claro, escuro e do sistema.
- Exportação e importação de backup em ZIP, incluindo o banco e as capas locais.

## Requisitos

- Windows 10 ou 11, 64 bits.
- SDK .NET 8 para compilar o projeto.
- Acesso ao NuGet para restaurar as dependências.
- Internet para consultar o AniList, carregar capas online ou traduzir sinopses.

## Executar e testar

No PowerShell, dentro da pasta do repositório:

```powershell
dotnet restore .\AnimeShelf.App\AnimeShelf.App.csproj
dotnet run --project .\AnimeShelf.App\AnimeShelf.App.csproj -c Release
dotnet test .\AnimeShelf.Tests\AnimeShelf.Tests.csproj -c Release
```

Para gerar uma pasta independente para Windows x64:

```powershell
dotnet publish .\AnimeShelf.App\AnimeShelf.App.csproj -c Release -r win-x64 --self-contained true -o .\publish\win-x64
```

Execute `AnimeShelf.App.exe` dentro da pasta gerada e mantenha os demais arquivos dessa pasta junto dele.

## Dados e tradução

O aplicativo guarda configurações, banco e capas em `%AppData%\AnimeShelf`. O backup não inclui os arquivos de vídeo nem as configurações; os caminhos dos episódios continuam apontando para o computador de origem.

A tradução da sinopse é opcional e usa um servidor LibreTranslate configurado pelo usuário. O servidor padrão exige uma chave de API, que é salva em texto simples nas configurações locais. Não compartilhe esse arquivo nem sua chave ao publicar o projeto. Títulos, gêneros e sinopses recebidos do AniList mantêm o idioma fornecido pela API até que uma tradução seja solicitada.

## Estrutura

- `AnimeShelf.App`: interface WPF, temas, idiomas e imagens do aplicativo.
- `AnimeShelf.Core`: varredura, SQLite e integração com o AniList.
- `AnimeShelf.Tests`: testes automatizados.

## Limitações conhecidas

A grade de capas ainda não é virtualizada e pode consumir mais memória em bibliotecas grandes. A associação de títulos é heurística e pode exigir correção manual. A janela de resultados do AniList ainda não mostra capas, e capas antigas podem permanecer no cache após uma correção. A importação de backup pode copiar algumas capas antes de concluir a troca do banco. A varredura não segue links simbólicos ou junções e marca itens como ausentes somente quando a pasta raiz foi percorrida sem erros.
