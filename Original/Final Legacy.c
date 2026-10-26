#include <stdio.h>
#include <locale.h>
#include <windows.h>
#include <stdlib.h>
#include <conio.h>
#include <string.h>
#include <unistd.h>

/* =Variáveis
----------------------------------------------------------------------------------------------------------------*/
int classe, arma;
char nome[30];
char nomeinimigo[30];

/* =[0] ataque , [1] defesa, [2] agilidade, [3] inteligencia
----------------------------------------------------------------------------------------------------------------*/
int stats[4];

/* =mudar o tamanho da consola e o nome da janela
----------------------------------------------------------------------------------------------------------------*/
void startup(){
    SMALL_RECT windowSize = {0, 0, 150, 30};
    SetConsoleWindowInfo(GetStdHandle(STD_OUTPUT_HANDLE), TRUE, &windowSize);
    SetConsoleTitle("-=| Final Legacy |=-");
}

/* =meter o texto a aparecer lentamente
----------------------------------------------------------------------------------------------------------------*/
void prints(char *s, unsigned ms_delay){
    unsigned usecs = ms_delay * 1000;
    for (; *s; s++)
    {
        putchar(*s);
        fflush(stdout);
        usleep(usecs);
    }
}

/* =mudar a cor do texto
----------------------------------------------------------------------------------------------------------------*/
void azul(){
    printf("\033[1;36m");
}
void branco(){
    printf("\033[0m");
}
void amarelo(){
    printf("\033[1;33m");
}
void torrado(){
    printf("\033[0;33m");
}
p void vermelho(){
    printf("\033[0;31m");
}
void vermelhob(){
    printf("\033[1;31m");
}
void roxo(){
    printf("\033[1;35m");
}

/*Sistema de combate
    char nomeinimigo [30] = "";
    int hpinimigo = 50;
    int ataqueinimigo = 7;
    int defesainimigo = 6;
    int persistencia = 1; 0 = podes fugir 1 = nao podes fugir*/
int combate(char nomeinimigo[30], int ataqueinimigo, int defesainimigo, int persistencia, int hpinimigo){
    int hpjogador = 100;
    int battlestats = 0; /*0 = batalha a decorrer 1 = win 2 = lose 3 = fugir*/
    int escolha;

    while (battlestats == 0){
        printf("\n\n A combater contra: %s", nomeinimigo);
        prints("\n\n [1] Atacar", 20);
        prints("\n\n [2] Defender", 20);
        prints("\n\n [3] Fugir\n\n", 20);

        int escolhainimigo = rand() % 2;

        scanf("%d", &escolha);
        switch (escolha){
        case 1:
        {
            if (escolhainimigo == 0){
                hpjogador -= ataqueinimigo;
                hpjogador = hpjogador < 0 ? 0 : hpjogador;
                hpinimigo -= stats[0];
                hpinimigo = hpinimigo < 0 ? 0 : hpinimigo;
                system("cls");
                printf("\n\n Inimigo Atacou !", 20);
            }
            else {
                int defesai = stats[0] - defesainimigo;
                defesai = defesai < 0 ? 0 : defesai;
                hpinimigo -= defesai;
                hpinimigo = hpinimigo < 0 ? 0 : hpinimigo;
                system("cls");
                prints("\n\n Inimigo Defendeu !", 20);
            }
            vermelhob();
            printf("\n\n HP");
            branco();
            printf(" Atual = %d\n\n ", hpjogador);
            vermelhob();
            printf("HP");
            branco();
            vermelho();
            printf(" %s", nomeinimigo);
            branco();
            printf(" = %d\n\n", hpinimigo);
        }
        break;
        case 2:
        {
            if (escolhainimigo == 0)
            {
                int defesai2 = ataqueinimigo - stats[1];
                defesai2 = defesai2 < 0 ? 0 : defesai2;
                hpjogador -= defesai2;
                hpjogador = hpjogador < 0 ? 0 : hpjogador;
                system("cls");
                prints("\n\n Inimigo Atacou !", 20);
            }
            else
            {
                system("cls");
                prints("\n\n Inimigo Defendeu !", 20);
            }
            vermelhob();
            printf("\n\n HP");
            branco();
            printf(" Atual = %d\n\n ", hpjogador);
            vermelhob();
            printf("HP");
            branco();
            vermelho();
            printf(" %s", nomeinimigo);
            branco();
            printf(" = %d\n\n", hpinimigo);
        }
        break;
        case 3:
        {
            if (persistencia == 0)
                battlestats = 3;
            else
                system("cls");
            prints("\n N�o podes fugir desta batalha...\n", 20);
        }
        break;
        default:
            system("cls");
            prints("\n\nIsso n�o � uma escolha v�lida.", 20);
            break;
        }
        if (hpjogador == 0)
        {
            battlestats = 2;
        }
        else if (hpinimigo == 0)
        {
            battlestats = 1;
        }
    }
    return battlestats;
}

int main()
{
    setlocale(LC_ALL, "Portuguese");

    /*Introduzir as variaveis para as batalhas*/
    int hpinimigo;
    int ataqueinimigo;
    int defesainimigo;
    int persistencia; /*0 = podes fugir 1 = nao podes fugir*/

    /*inserir os codigos anteriores fora do int*/
    startup();

    do
    {
        /*introdu��o*/
        system("cls");
        prints("\n\n -----|Bem-Vindo ao ", 50);
        amarelo();
        prints("Final Legacy", 50);
        branco();
        prints("!|-----\n\n\n\tEscolha a sua classe:", 50);

        /*escolha de classe*/
        prints("\n\n [1] ", 50);
        vermelhob();
        prints("Guerreiro", 50);
        branco();
        prints(": 4 ATK 5 DEF 2 AGI 2 INT\n\n", 50);
        prints("\n [2] ", 50);
        azul();
        prints("Mago", 50);
        branco();
        prints(": 3 ATK 3 DEF 1 AGI 6 INT\n\n", 50);
        prints("\n [3] ", 50);
        roxo();
        prints("Assassino", 50);
        branco();
        prints(": 5 ATK 1 DEF 5 AGI 2 INT\n\n", 50);
        scanf("%d", &classe);

        /*escolha de armas*/
        switch (classe)
        {
        case 1:
            stats[0] = 4;
            stats[1] = 5;
            stats[2] = 2;
            stats[3] = 2;

            do
            {
                /*escolha de armas*/
                prints("\n Prepare seu grito de guerra! Agora passemos para a escolha da arma...\n", 50);
                prints("\n [1] ", 50);
                azul();
                prints("Escudo de Dorans", 50);
                branco();
                prints(" +2 DEF -1 ATK\n\n [2] ", 50);
                azul();
                prints("Espada de Art�rias", 50);
                branco();
                prints(" +2 ATK -1 DEF\n\n", 50);
                scanf("%d", &arma);

                /*resumo da classe*/
                switch (arma)
                {
                case 1:
                    stats[0] = 4 - 1;
                    stats[1] = 5 + 2;
                    stats[2] = 2;
                    stats[3] = 2;

                    prints("\nClasse escolhida: ", 50);
                    vermelhob();
                    prints("Guerreiro", 50);
                    branco();
                    prints(";\n\nArma escolhida: ", 50);
                    azul();
                    prints("Escudo de Dorans", 50);
                    branco();
                    prints(";\n", 50);
                    system("pause");
                    if (arma = 1)
                        goto historia;
                    break;
                case 2:
                    stats[0] = 4 + 2;
                    stats[1] = 5 - 1;
                    stats[2] = 2;
                    stats[3] = 2;

                    prints("\nClasse escolhida: ", 50);
                    vermelhob();
                    prints("Guerreiro", 50);
                    branco();
                    prints(";\n\nArma escolhida: ", 50);
                    azul();
                    prints("Espada de Art�rias", 50);
                    branco();
                    prints(";\n", 50);
                    system("pause");
                    if (arma = 2)
                        goto historia;
                    break;
                default:
                {
                    system("cls");
                    prints("\n\n Escolha inv�lida! Tente usar um dos n�meros apresentados!\n\n", 50);
                }
                }
            } while (arma != 1 || arma != 2);

            break;
        case 2:
            stats[0] = 3;
            stats[1] = 3;
            stats[2] = 1;
            stats[3] = 6;

            do
            {
                /*escolha de armas*/
                prints("\n Escolha perpicaz para jogadores experientes! Agora passemos para a escolha das suas magias...\n", 50);
                prints("\n [1] ", 50);
                azul();
                prints("Anel de Dorans ", 50);
                branco();
                prints("+2 INT -1 ATK\n\n [2] ", 50);
                azul();
                prints("Bast�o Desnecess�riamente Grande ", 50);
                branco();
                prints("+2 INT -1 AGI\n\n", 50);
                scanf("%d", &arma);

                /*resumo da classe*/
                switch (arma)
                {
                case 1:
                    stats[0] = 3 - 1;
                    stats[1] = 3;
                    stats[2] = 1;
                    stats[3] = 6 + 2;

                    prints("\nClasse escolhida: ", 50);
                    azul();
                    prints("Mago", 50);
                    branco();
                    prints(";\n\nArma escolhida: ", 50);
                    azul();
                    prints("Anel de Dorans", 50);
                    branco();
                    prints(";\n", 50);
                    system("pause");
                    if (arma = 1)
                        goto historia;
                    break;
                case 2:
                    stats[0] = 3;
                    stats[1] = 3;
                    stats[2] = 1 - 1;
                    stats[3] = 6 + 2;

                    prints("\nClasse escolhida: ", 50);
                    azul();
                    prints("Mago", 50);
                    branco();
                    prints(";\n\nArma escolhida: ", 50);
                    azul();
                    prints("Bast�oo Desnecess�riamente Grande", 50);
                    branco();
                    prints(";\n", 50);
                    system("pause");
                    if (arma = 2)
                        goto historia;
                    break;
                default:
                {
                    system("cls");
                    prints("\n\n Escolha inv�lida! Tente usar um dos n�meros apresentados!\n\n", 50);
                }
                }
            } while (arma != 1 || arma != 2);

            break;
        case 3:
            stats[0] = 5;
            stats[1] = 1;
            stats[2] = 5;
            stats[3] = 2;

            do
            {
                /*escolha de armas*/
                prints("\n Perfeito para mortes r�pidas e silenciosas! Adiante para a escolha das facas..\n", 50);
                prints("\n [1] ", 50);
                azul();
                prints("Faca de Dorans", 50);
                branco();
                prints(" +2 ATK -1 INT\n\n [2] ", 50);
                azul();
                prints("L�mina de Shojin ", 50);
                branco();
                prints("+2 ATK -1 DEF\n\n", 50);
                scanf("%d", &arma);

                /*resumo da classe*/
                switch (arma)
                {
                case 1:
                    stats[0] = 5 + 2;
                    stats[1] = 1;
                    stats[2] = 5;
                    stats[3] = 2 - 1;

                    prints("\nClasse escolhida: ", 50);
                    roxo();
                    prints("Assassino", 50);
                    branco();
                    prints(";\n\nArma escolhida: ", 50);
                    azul();
                    prints("Faca de Dorans", 50);
                    branco();
                    prints(";\n", 50);
                    system("pause");
                    if (arma = 1)
                        goto historia;
                    break;
                case 2:
                    stats[0] = 5 + 2;
                    stats[1] = 1 - 1;
                    stats[2] = 5;
                    stats[3] = 2;

                    prints("\nClasse escolhida: ", 50);
                    roxo();
                    prints("Assassino", 50);
                    branco();
                    prints(";\n\nArma escolhida: ", 50);
                    azul();
                    prints("L�mina de Shojin", 50);
                    branco();
                    prints(";\n", 50);
                    system("pause");
                    if (arma = 2)
                        goto historia;
                    break;
                default:
                {
                    system("cls");
                    prints("\n\n Escolha inv�lida! Tente usar um dos n�meros apresentados!\n\n", 50);
                }
                }
            } while (arma != 1 || arma != 2);

            break;
        default:
        {
            system("cls");
            prints("\n\n Escolha inv�lida! Tente usar um dos n�meros apresentados!\n\n", 50);
        }
        }
    } while (classe != 1 || classe != 2 || classe != 3);

/*inicio da historia*/
historia:
    system("cls");
    prints("\nApenas nos falta o teu nome: \n\n", 50);
    scanf("%s", &nome);
    system("cls");
    torrado();
    prints("\n ", 50);
    prints(nome, 50);
    branco();
    prints("... ", 500);
    prints("esse nome ser� relembrado...\n", 50);
    system("pause");
    system("cls");
    printf("\n");
    torrado();
    prints(nome, 50);
    branco();
    prints(", acordas-te numa caverna escura, ao que parece n�o sabes como foste a� parar.\nAo olhares para longe encontras uma luz que aparenta ser uma sa�da.\nProsseguir ?", 50);

    //escolhas

    int escolha;
    do
    {
        prints("\n\n [1] Sim\t\t [2] N�o\n\n", 50);
        scanf("%d", &escolha);

        switch (escolha)
        {
        case 1:
            prints("\n�timo. Ao avan�ares para o fim da caverna, deparas-te com um ", 50);
            vermelho();
            prints("Esqueleto", 50);
            branco();
            prints(" que ao que parece te quer matar.\nN�o sendo isso l� muito bom, vais ter que lutar contra ele !\n", 50);
            system("pause");
            system("cls");
            break;
        case 2:
            prints("Ahmnnn... acho que assim n�o vamos sa�r daqui t�o cedo...", 50);
            break;
        }
    } while (escolha != 1);

    nomeinimigo[30] == "Esqueleto";
    char nomeinimigotemp[30] = "Esqueleto";
    strcpy(nomeinimigo, nomeinimigotemp);
    hpinimigo = 50;
    ataqueinimigo = 7;
    defesainimigo = 1;
    persistencia = 1; /*0 = podes fugir 1 = nao podes fugir*/

    switch (combate(nomeinimigo, ataqueinimigo, defesainimigo, persistencia, hpinimigo))
    {
    case 1:
    {
        torrado();
        prints("Ganhaste", 50);
        branco();
        prints(" a batalha !\n", 50);
        system("pause");
    }
    break;
    case 2:
    {
        vermelho();
        prints("Perdeste", 50);
        branco();
        prints(" a batalha.\n", 50);
        system("pause");
        return 0;
    }
    break;
    case 3:
    {
        prints("Fugiste com sucesso !\n", 50);
        system("pause");
    }
    break;
    default:
    {
        prints("Erro", 50);
        return -1;
    }
    break;
    }
    system("cls");

    prints("Excelente, conseguiste matar o esqueleto ! Devia estar com Osteoperose\n\nhaha... \n\nn�o ... ? \n\nok... \n\ndesculpa.\n\nAdiante ! Ao prosseguires para fora da gruta d�s de caras com uma vasta\ncidade", 50);
    prints(" que parece estar abandonada. Queres ir explorar ?", 50);

    int escolha2;
    do
    {
        prints("\n\n [1] Sim\t\t [2] N�o\n\n", 50);
        scanf("%d", &escolha2);

        switch (escolha2)
        {
        case 1:
            system("cls");
            prints("\n\nAo avan�ares para o fundo da cidade, encontras uma sombra flutoante longinqua estranha.\nAo ires ver o que se passa, ela quase que te mata com uma lan�a que te trespassa a lateral do peito.\nVais ter que lutar !\n", 50);
            system("pause");
            system("cls");

            nomeinimigo[30] == "Sombra Misteriosa";
            char nomeinimigotemp[30] = "Sombra Misteriosa";
            strcpy(nomeinimigo, nomeinimigotemp);
            hpinimigo = 50;
            ataqueinimigo = 9;
            defesainimigo = 2;
            persistencia = 1; /*0 = podes fugir 1 = nao podes fugir*/

            switch (combate(nomeinimigo, ataqueinimigo, defesainimigo, persistencia, hpinimigo))
            {
            case 1:
            {
                torrado();
                prints("Ganhaste", 50);
                branco();
                prints(" a batalha !\n", 50);
                system("pause");
                goto final;
            }
            break;
            case 2:
            {
                vermelho();
                prints("Perdeste", 50);
                branco();
                prints(" a batalha.\n", 50);
                system("pause");
                return 0;
            }
            break;
            case 3:
            {
                prints("Fugiste com sucesso !\n", 50);
                system("pause");
            }
            break;
            default:
            {
                prints("Erro", 50);
                return -1;
            }
            break;
            }
            system("cls");
            break;
        case 2:
            system("cls");
            prints("\n\n Ok ent�o, ao percorreres a floresta encontras um monte de pedras que usas para descansares um pouco. \nAo te sentares a pedra levanta-se e n�o parece muito contente contigo... \nVais ter que lutar !\n", 50);
            system("pause");
            system("cls");

            nomeinimigo[30] == "Golem de Pedra";
            char nomeinimigotemp2[30] = "Golem de Pedra";
            strcpy(nomeinimigo, nomeinimigotemp2);
            hpinimigo = 50;
            ataqueinimigo = 9;
            defesainimigo = 2;
            persistencia = 1; /*0 = podes fugir 1 = nao podes fugir*/

            switch (combate(nomeinimigo, ataqueinimigo, defesainimigo, persistencia, hpinimigo))
            {
            case 1:
            {
                torrado();
                prints("Ganhaste", 50);
                branco();
                prints(" a batalha !\n", 50);
                system("pause");
                goto final;
            }
            break;
            case 2:
            {
                vermelho();
                prints("Perdeste", 50);
                branco();
                prints(" a batalha.\n", 50);
                system("pause");
                return 0;
            }
            break;
            case 3:
            {
                prints("Fugiste com sucesso !\n", 50);
                system("pause");
            }
            break;
            default:
            {
                prints("Erro", 50);
                return -1;
            }
            break;
            }
            system("cls");
            break;
        default:
            system("cls");
            prints("\n\n�s f� das escolha inv�lidas pelo que estou a ver !", 50);
            break;
        }
    } while (escolha2 != 1 || escolha2 != 2);

final:
    system("cls");
    prints("\n\nIsto foi uma demostra��o de um jogo para o projeto da escola ! Para a continua��o da hist�ria v�o ter de esperar um pouco mais !", 50);
}
