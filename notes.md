# Dia 1
encontrei um objeto Vehicle pelo Runtime Unity Editor. Olhando pelo dnSpy, acho que ele é uma instância de VehicleModel (não tenho certeza porque não entendi como checa a classe exata no RUE).

descobri que, no dnspy, dá pra clicar com o botão direito na classe e clicar em Analyze pra encontrar de verdade todas as referências a ela. Uma interessante pro futuro: Motorways.Processes.VehicleMovementProcess._stuckVehicles : List<VehicleModel>.


# Dia 2
achei um GetModels<T> dentro do Server.Simulation, que seria uma forma mais robusta de listar todos os VehicleModel, mas não encontrei onde o Simulation é instanciado. Vou seguir pela abordagem de encontrar os objetos do tipo VehicleView e partir disso.

ainda não encontrei uma forma de filtrar todos os veículos realmente ativos. filtrar pelas propriedades booleanas ou pelos atributos de house ou de posição zeradas não resolve.


# Dia 3
pensando com a cabeça fresca, resolvi o problema de que apareciam mais veículos do que realmente existia na cidade: o menu do jogo tem uma simulaçãozinha rodando, então existiam carros ativos nele. filtrei os carros cuja cidade eram `MenuCity` e resolveu.

montei socket server/client e consegui puxar os dados pelo python. estou commitando.