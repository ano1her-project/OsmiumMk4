using Osmium.Core;
using Osmium.Minimax;
using Osmium.Heuristics;
using System.Diagnostics;

namespace Osmium.Interface;

internal class Program
{/*
    static Position position = Position.startingPosition;
    static bool enginePlayingWhite, enginePlayingBlack;
    static bool engineMovesAutomatically;
    static int depth = 3;
    static int evalSortDepth = 3;
    static Minimax.Minimax.DebugPrintMode minimaxDebugPrintMode = Minimax.Minimax.DebugPrintMode.Elaborate;*/

    static void Main()
    {
        Console.ReadLine();
    }/*

    static void CommandLoop()
    {
        if (engineMovesAutomatically && ((enginePlayingWhite && position.whiteToMove) || (enginePlayingBlack && !position.whiteToMove)))
        {
            var sw = Stopwatch.StartNew();
            var bestMove = Minimax.Minimax.FindBestMove(position, depth, evalSortDepth, minimaxDebugPrintMode, out int eval);
            var time = sw.Elapsed;
            position.MakeMove(bestMove, out _);
            Console.WriteLine($"Found best move {bestMove} in {time} and played it. Eval = {eval}.");
            PrettyPrinter.Print(position);
        }
        Console.Write("> ");
        string input = Console.ReadLine();
        var subs = input.Split();
        switch (subs[0])
        {
            case "help":
                Console.WriteLine("help                                  - Lists all commands.");
                Console.WriteLine("abrreviate (name of command)          - Lists abbreviations for a command.");
                Console.WriteLine("set_position (fen)                    - Sets the current position to the provided FEN.");
                Console.WriteLine("set_position (starting | kiwipete)    - Sets the current position to a special pick.");
                Console.WriteLine("set_white_player (engine | player)    - Sets who controls the white pieces.");
                Console.WriteLine("set_black_player (engine | player)    - Sets who controls the black pieces.");
                Console.WriteLine("set_engine_automatic_moves (yes | no) - Sets whether the engine waits for the let_engine_make_move command.");
                Console.WriteLine("set_depth (depth)                     - Sets the engine search depth.");
                Console.WriteLine("get_fen                               - Prints the FEN of the current position.");
                Console.WriteLine("estimate                              - Calculates the static evaluation of the current position.");
                Console.WriteLine("evaluate                              - Evaluates the current position.");
                Console.WriteLine("perft (depth)                         - Counts leaf nodes at a given depth.");
                Console.WriteLine("let_engine_make_move                  - Has the engine choose and play the best move.");
                Console.WriteLine("(square in algebraic format)          - Starts a move.");

                break;
            case "abbreviate":
                Console.WriteLine(subs[1] switch
                {
                    "help" => "no abbreviation.",
                    "abbreviate" => "no abbrevioation",
                    "set_position" => "position, pos",
                    "set_white_player" => "white",
                    "set_black_player" => "black",
                    "set_engine_automatic_moves" => "auto",
                    "set_depth" => "depth, d",
                    "get_fen" => "fen",
                    "estimate" => "est",
                    "evaluate" => "eval",
                    "perft" => "no abbreviation.",
                    "let_engine_make_move" => "lemm",
                    _ => "command doesn't exist."
                });
                break;
            case "set_position":
            case "position":
            case "pos":
                if (subs[1] == "startpos" || subs[1] == "start")
                {
                    position = Position.startingPosition.DeepCopy();
                    PrettyPrinter.Print(position);
                }
                else if (subs[1] == "kiwipete")
                {
                    position = Position.FromFEN("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 99 99");
                    PrettyPrinter.Print(position);
                }
                else if (subs.Length != 7)
                    Console.WriteLine("invalid argument");
                else
                {
                    // we join the split subs back together,, probably not ideal but whatever
                    position = Position.FromFEN($"{subs[1]} {subs[2]} {subs[3]} {subs[4]} {subs[5]} {subs[6]}");
                    PrettyPrinter.Print(position);
                }
                break;
            case "set_white_player":
            case "white":
                switch (subs[1])
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
            case "set_black_player":
            case "black":
                switch (subs[1])
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
            case "set_engine_automatic_moves":
            case "auto":
                switch (subs[1])
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
            case "set_depth":
            case "depth":
            case "d":
                depth = int.Parse(subs[1]);
                break;
            case "set_eval_sort_depth":
            case "esd":
                evalSortDepth = int.Parse(subs[1]);
                break;
            case "get_fen":
            case "fen":
                Console.WriteLine(position.ToFEN());
                break;
            case "estimate":
            case "est":
                Console.WriteLine($"Estimated evaluation: {Heuristics.Evaluate(position)}.");
                break;
            case "evaluation":
            case "eval":
                Console.WriteLine($"Evaluation: {Minimax.Minimax.Evaluate(position, depth, int.MinValue, int.MaxValue)}");
                break;
            case "perft":
                Console.WriteLine("total amount: " + Perft.CountLeafNodesAtDepthByMove(position, int.Parse(subs[1])));
                break;
            case "let_engine_make_move":
            case "lemm":
                var sw = Stopwatch.StartNew();
                var bestMove = Minimax.Minimax.FindBestMove(position, depth, evalSortDepth, minimaxDebugPrintMode, out int eval);
                var time = sw.Elapsed;
                position.MakeMove(bestMove, out _);
                Console.WriteLine($"Found best move {bestMove} in {time} and played it. Eval = {eval}.");
                PrettyPrinter.Print(position);
                break;
            default: // a move:
                if (subs.Length > 1)
                {
                    Console.WriteLine("invalid command.");
                    break;
                }
                var from = Vector2.FromString(input);
                var piece = position.GetPiece(from);
                if (piece is null)
                {
                    Console.WriteLine("square is empty. aborting move.");
                    break;
                }
                if (piece?.isWhite != position.whiteToMove)
                {
                    Console.WriteLine("piece color doesn't match color to move. aborting move");
                    break;
                }
                var moves = position.FilterLegalMoves(position.GetPieceMoves((Piece)piece, from));
                var tos = new Vector2[moves.Count];
                for (int i = 0; i < moves.Count; i++)
                    tos[i] = moves[i].to;
                PrettyPrinter.Print(position, from, tos);
                var to = Vector2.FromString(Console.ReadLine());
                int moveIndex = Array.IndexOf(tos, to);
                if (moveIndex == -1)
                {
                    Console.WriteLine("invalid destination for this piece. aborting move.");
                    break;
                }
                position.MakeMove(moves[moveIndex], out _);
                PrettyPrinter.Print(position);
                break;
        }
        CommandLoop();
    }*/
}

public class PrettyPrinter
{
    public static void Print(ulong bitboard)
    {
        for (int rank = 7; rank >= 0; rank--)
        {
            string s = (rank + 1) + " ";
            for (int i = rank * 8; i < (rank + 1) * 8; i++)
                s += (((1ul << i) & bitboard) == 0) ? "0 " : "1 ";
            Console.WriteLine(s);
        }
        Console.WriteLine("  a b c d e f g h ");
    }

    public static void PrintBitboardByBitboard(Position position)
    {
        for (PieceType piece = 0; (int)piece < 6; piece++)
        {
            Console.WriteLine(piece.ToString() + "s");
            Print(position.GetPieceBitboard(piece));
        }
        for (PieceColor color = 0; (int)color < 2; color++)
        {
            Console.WriteLine(color.ToString());
            Print(position.GetColorBitboard(color));
        }
        Console.WriteLine("Empty");
        Print(position.GetEmptySquareSet());
    }
}

