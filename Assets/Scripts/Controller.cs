using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;
    List<int> casillasRobber = new List<int>();


    void Start()
    {
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }

    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();
            }
        }

        cops[0].GetComponent<CopMove>().currentTile = Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile = Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile = Constants.InitialRobber;
    }

    public void InitAdjacencyLists()
    {
        //Matriz de adyacencia
        int[,] matriz = new int[Constants.NumTiles, Constants.NumTiles];

        // Inicializar matriz a 0's

        for (int i = 0; i < Constants.NumTiles; i++)
        {
            for (int j = 0; j < Constants.NumTiles; j++)
            {
                matriz[i, j] = 0;
            }
        }

        //Para cada posición, rellenar con 1's las casillas adyacentes (arriba, abajo, izquierda y derecha)
        //Rellenar la lista "adjacency" de cada casilla con los índices de sus casillas adyacentes

        int t = 0;
        for (int x = 0; x < Constants.NumTiles; x++)
        {
            for (int c = 0; c < Constants.NumTiles; c++)
            {

                if (c > Constants.NumTiles || c < 0)
                {
                }
                else if (c == x + 1 || c == x - 1 || c == x + 8 || c == x - 8)
                {
                    matriz[x, c] = 1;
                    tiles[x].adjacency.Add(c);
                }

                int x2 = x + 1;
                if (x2 % 8 == 0 && x != 0 && x2 < Constants.NumTiles)
                {
                    tiles[x].adjacency.Remove(x2);
                    matriz[x, x2] = 0;
                }

                int x3 = x - 1;
                if (x % 8 == 0 && x != 0)
                {
                    tiles[x].adjacency.Remove(x - 1);
                    matriz[x, x - 1] = 0;
                }


                t++;
            }
        }
    } // InitAdjacencyLists()

    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;
                break;
        }
    }

    public void ClickOnTile(int t)
    {
        clickedTile = t;

        switch (state)
        {
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile = tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;

                    state = Constants.TileSelected;
                }
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        Debug.Log("Robbers turn");

        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false);


        /* 
        - Elegimos una casilla aleatoria entre las seleccionables que puede ir el caco
        - Movemos al caco a esa casilla
        - Actualizamos la variable currentTile del caco a la nueva casilla
        */

        for (int i = 0; i < casillasRobber.Count; i++)
        {
            Debug.Log(casillasRobber[i]);
            if (tiles[i].selectable)
            {
                casillasRobber.Add(i);
            }
        }

        System.Random ran = new System.Random();

        int num = ran.Next(casillasRobber.Count);
        int randomList = casillasRobber[num];

        Debug.Log("Random number --> " + randomList);

        robber.GetComponent<RobberMove>().currentTile = randomList;

        robber.GetComponent<RobberMove>().MoveToTile(tiles[randomList]);
    } // RobberTurn()

    public void EndGame(bool end)
    {
        if (end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);

        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;

    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {

        int indexcurrentTile;

        if (cop == true) //nodo fuente
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        casillasRobber.Clear();

        //La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;

        //Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();

        // Implementar BFS. Los nodos seleccionables los ponemos como selectable=true
        // Tendrás que cambiar este código por el BFS

        tiles[indexcurrentTile].visited = true;
        tiles[indexcurrentTile].distance = 0;
        tiles[indexcurrentTile].parent = null;

        nodes.Enqueue(tiles[indexcurrentTile]);

        Tile antes;

        while (nodes.Count != 0)
        {
            antes = nodes.Dequeue();
            int antes2 = antes.numTile;

            foreach (int adyacente in tiles[antes2].adjacency)
            {
                if (tiles[adyacente].visited == false)
                {
                    tiles[adyacente].visited = true;
                    tiles[adyacente].distance = tiles[antes2].distance + 1;
                    tiles[adyacente].parent = tiles[antes2];
                    nodes.Enqueue(tiles[adyacente]);

                    if (tiles[adyacente].distance <= 2)
                    {
                        if (cop == false)
                        {
                            casillasRobber.Add(tiles[adyacente].numTile);
                        }
                        tiles[adyacente].selectable = true;
                        if (cops[0].GetComponent<CopMove>().currentTile == tiles[adyacente].numTile || cops[1].GetComponent<CopMove>().currentTile == tiles[adyacente].numTile)
                        {
                            tiles[adyacente].selectable = false;
                        }
                    }

                }
            }

        }
    } // FindSelectableTiles()
}