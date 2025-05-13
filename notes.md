# Dia 1
encontrei um objeto Vehicle pelo Runtime Unity Editor. Olhando pelo dnSpy, acho que ele é uma instância de VehicleModel (não tenho certeza porque não entendi como checa a classe exata no RUE).

descobri que, no dnspy, dá pra clicar com o botão direito na classe e clicar em Analyze pra encontrar de verdade todas as referências a ela. Uma interessante pro futuro: Motorways.Processes.VehicleMovementProcess._stuckVehicles : List<VehicleModel>.


# Dia 2
achei um GetModels<T> dentro do Server.Simulation, que seria uma forma mais robusta de listar todos os VehicleModel, mas não encontrei onde o Simulation é instanciado. Vou seguir pela abordagem de encontrar os objetos do tipo VehicleView e partir disso.

ainda não encontrei uma forma de filtrar todos os veículos realmente ativos. filtrar pelas propriedades booleanas ou pelos atributos de house ou de posição zeradas não resolve.


# Dia 3
pensando com a cabeça fresca, resolvi o problema de que apareciam mais veículos do que realmente existia na cidade: o menu do jogo tem uma simulaçãozinha rodando, então existiam carros ativos nele. filtrei os carros cuja cidade eram `MenuCity` e resolveu.

montei socket server/client e consegui puxar os dados pelo python. estou commitando.

a captura das casas tá indo bem, mas o DestinationView não tem o mesmo atributo `tilePosition`. provavelmente vou ter que chamar o método `GetBounds()` ou reproduzir parte da lógica interna dele (`Vector2Int coordinates = this.Model.TileModels[0].Coordinates;`)

pra acelerar o jogo pra treinamento, encontrei o método `OnExtraFastForwardPressed()` do `Motorways.Views.GameUIScreen`, que chama o método `SetTimeScale` da instância de `Game`. o problema é que Game não é MonoBehaviour e não encontrei onde ele é instanciado.

existe uma instância de Game dentro do GameContainerScreen, que é um MonoBehaviour. por ela, consigo setar a velocidade do jogo, mas o Game só é instanciado quando a partida de fato começa, então preciso de uma forma de setar isso quando a partida começar pra não precisar setar isso dentro do update toda vez.


# Dia 4
Peguei a lib FixedMath.dll do jogo e deixei como dependência pra conseguir loggar alguns valores. BuildingSpawningProcess parece uma classe útil pra entender comportamentos do relógio.


# Dia 5
pausar quando vira um dia está funcional! agora devo alterar para, em vez de chamar o `SetTimeScale`, usar os métodos `OnExtraFastForwardPressed` e `OnPausePressed` da classe `Motorways.Views.GameUIScreen` (ou talvez `Motorways.Views.GameUIScreenWrapper`, ainda não sei). também precisaria alterar o valor do `TimeScale.ExtraFast`.


# Dia 6
lembrar de, no futuro, investigar o RNG do jogo: inicialmente, talvez seja interessante fixar o RNG. de qualquer forma, para fins de reprodutibilidade, precisarei ter controle do RNG.

refatorei o código pra facilitar no futuro.

# Dia 7
deixei o speedup automático para quando o servidor retornar um ACK. futuramente preciso mudar isso para uma ação. acredito que o próximo passo seria melhorar o estado do jogo (faltam os recursos disponíveis e o score). para a escolha de recurso, provavelmente vou usar uma heurística pra não aumentar o espaço de ações (e introduzir um action masking desnecessariamente complexo).

# Referências
https://github.com/paulalmasan/DRL-GNN-PPO