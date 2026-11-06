# Pokémon ShowUp

Batalhas Pokémon com times de seis, IA, dois jogadores no mesmo console, partidas em rede e campanha Jornada de nove regiões.

Este é o repositório de distribuição dos pacotes preparados no projeto R36S. A versão interna do jogo é **0.5.0+r36s-audio.20261012**; a release de distribuição é de 06/11/2026.

## Baixar

[Pacote R36S ARM64](https://github.com/EoTutts/pokemon-showup/releases/download/v0.5.0-r36s-audio.20261012-distribution.20261106/pokemon-showup-linux-arm64.zip) · [Release e SHA-256](https://github.com/EoTutts/pokemon-showup/releases/tag/v0.5.0-r36s-audio.20261012-distribution.20261106)

## Requisitos e estado

Luma/ArkOS em R36S **RK3326, Linux ARM64**, com as bibliotecas de áudio e vídeo da plataforma. O pacote usa o runtime .NET 10 compartilhado do Luma. Não é um pacote para GA36/ARMv7. Distribuição experimental. O pacote foi compilado para Linux ARM64; a validação física desta revisão no console permanece pendente.

## Instalar ou atualizar

Extraia o ZIP inteiro e copie a pasta `pokemon-showup` para `a11y/games/` na partição EASYROMS do cartão. O `game.json` deve ficar diretamente dentro dessa pasta. Com Luma ligado, a lista reconhece o jogo automaticamente. Também é possível enviar o ZIP pela opção Instalar jogo da Página no PC do Luma. Feche o jogo antes de atualizar; preserve a pasta `a11y/saves/`.

## Controles

Direcional navega; B confirma; A volta; Start pausa. Os menus e a Jornada têm navegação falada. Fn, Power e volume continuam reservados ao sistema.

## Arquivos e salvamentos

O pacote foi conferido quanto ao manifesto, executável e SHA-256. Não acompanha salvamentos, preferências pessoais ou registros de testes. Os saves ficam na pasta de dados atribuída pelo Luma, normalmente `a11y/saves/pokemon-showup/`. Esta publicação não representa uma nova campanha completa ou teste físico.

## Créditos e licenças

O motor Pokémon Showdown é MIT. Pokémon, imagens, músicas e sons têm direitos e créditos próprios; consulte CREDITS.md, GEN9-CREDITS.txt e os demais créditos do pacote. Os avisos de terceiros presentes no pacote devem ser mantidos. Não se atribui uma licença única a todo o conteúdo.
