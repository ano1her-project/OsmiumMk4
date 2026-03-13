using Osmium.Core;
using Osmium.Minimax;
using Osmium.Heuristics;
using System.Diagnostics;
using System.ComponentModel.Design;

namespace Osmium.Interface;

internal class Program
{
    static Position position = Position.StartingPosition();
    static bool enginePlayingWhite, enginePlayingBlack;
    static bool engineMovesAutomatically;
    static int depth = 3;

    static bool printPosition = true;

    static void Main()
    {
        while (true)
            CommandLoop();
    }

    static void CommandLoop()
    {
        if (printPosition)
        {
            PrettyPrinter.Print(position);
            printPosition = false;
        }
        if ((position.colorToMove == PieceColor.White && enginePlayingWhite) ||
            (position.colorToMove == PieceColor.Black && enginePlayingBlack))
            LetEngineMakeMove();
        //
        Console.Write("> ");
        string input = Console.ReadLine();
        var subs = input.Split();
        switch (subs[0])
        {
            case "help":
                Console.WriteLine("help                                  - Lists all commands.");
                Console.WriteLine("set position (fen)                    - Sets the current position to the provided FEN.");
                Console.WriteLine("set position (startpos | kiwipete)    - Sets the current position to a special pick.");
                Console.WriteLine("set white (engine | player)           - Sets who controls the white pieces.");
                Console.WriteLine("set black (engine | player)           - Sets who controls the black pieces.");
                Console.WriteLine("set auto (yes | no)                   - Sets whether the engine waits for the engine make move command during its turn.");
                Console.WriteLine("set depth (value)                     - Sets the engine search depth.");
                Console.WriteLine("fen                                   - Prints the FEN of the current position.");
                Console.WriteLine("heuristic                             - Calculates the heuristic evaluation of the current position.");
                Console.WriteLine("evaluate                              - Evaluates the current position using minimax.");
                Console.WriteLine("perft (depth)                         - Counts leaf nodes at a given depth.");
                Console.WriteLine("engine make move                      - Has the engine choose and play the best move.");
                Console.WriteLine("engine find move                      - Only has the engine suggest and print a move.");
                Console.WriteLine("(square in algebraic format)          - Starts a move.");
                break;
            case "set":
                SetOption(subs);
                break;
            case "fen":
                //Console.WriteLine(position.ToFEN());
                break;
            case "heuristic":
                Console.WriteLine($"Heuristic evaluation = {Heuristics.Heuristics.Evaluate(position)}.");
                break;
            case "evaluate":
                Console.WriteLine($"Evaluation = {Minimax.Minimax.Evaluate(position, depth, int.MinValue, int.MaxValue)}");
                break;
            case "perft":
                Console.WriteLine("total amount: " + Perft.CountLeafNodesAtDepth(position, int.Parse(subs[1])));
                break;
            case "engine":
                if (subs[1] == "make")
                    LetEngineMakeMove();
                else if (subs[1] == "find")
                {
                    var sw = Stopwatch.StartNew();
                    var bestMove = Minimax.Minimax.FindBestMove(position, depth, out int eval);
                    var time = sw.Elapsed;
                    Console.WriteLine($"Found best move {bestMove} in {time}. Eval = {eval}.");
                }
                break;
        }
    }

    static void SetOption(string[] subs)
    {
        switch (subs[1])
        {
            case "position":                
                if (subs[2] == "startpos" || subs[1] == "start")
                    position = Position.StartingPosition();
                else if (subs[2] == "kiwipete")
                    position = Position.FromFEN("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 99 99");
                else if (subs.Length != 8)
                {
                    Console.WriteLine("invalid argument.");
                    return;
                }
                else
                    position = Position.FromFEN($"{subs[2]} {subs[3]} {subs[4]} {subs[5]} {subs[6]} {subs[7]}");
                printPosition = true;
                break;
            case "white":
                switch (subs[2])
                {
                    case "engine":
                        enginePlayingWhite = true;
                        break;
                    case "player":
                        enginePlayingWhite = false;
                        break;
                    default:
                        Console.WriteLine("invalid argument.");
                        break;
                }
                break;
            case "black":
                switch (subs[2])
                {
                    case "engine":
                        enginePlayingBlack = true;
                        break;
                    case "player":
                        enginePlayingBlack = false;
                        break;
                    default:
                        Console.WriteLine("invalid argument.");
                        break;
                }
                break;
            case "auto":
                switch (subs[2])
                {
                    case "yes":
                        engineMovesAutomatically = true;
                        break;
                    case "no":
                        engineMovesAutomatically = false;
                        break;
                    default:
                        Console.WriteLine("invalid argument.");
                        break;
                }
                break;
            case "depth":
                depth = int.Parse(subs[2]);
                break;
            default:
                Console.WriteLine("invalid argument.");
                break;
        }
    }

    static void LetEngineMakeMove()
    {
        var sw = Stopwatch.StartNew();
        var bestMove = Minimax.Minimax.FindBestMove(position, depth, int.MinValue, int.MaxValue, out int eval);
        var time = sw.Elapsed;
        position.MakeMove(bestMove, out _);
        Console.WriteLine($"Found best move {bestMove} in {time} and played it. Eval = {eval}.");
        printPosition = true;
    }
}