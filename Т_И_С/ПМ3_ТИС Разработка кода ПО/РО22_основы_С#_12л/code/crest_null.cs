using System;
internal class Program
{
    static char[] board = { '1', '2', '3', '4', '5', '6', '7', '8', '9' };
    static int currentPlayer = 1;

    static void Main()
    {
        int gameStatus = 0;

        do
        {
            Console.Clear();
            DisplayBoard();
            int choice = GetPlayerChoice();
            MakeMove(choice);
            gameStatus = CheckWin();
            currentPlayer = (currentPlayer % 2) + 1;
        }
        while (gameStatus == 0);

        Console.Clear();
        DisplayBoard();
        if (gameStatus == 1)
        {
            Console.WriteLine($"Игрок {currentPlayer} выиграл!");
        }
        else
        {
            Console.WriteLine("Ничья!");
        }
    }

    static void DisplayBoard()
    {
        Console.WriteLine("Крестики-нолики");
        Console.WriteLine("Игрок 1: X | Игрок 2: O");
        Console.WriteLine("     |     |     ");
        Console.WriteLine($"  {board[0]}  |  {board[1]}  |  {board[2]} ");
        Console.WriteLine("_____|_____|_____");
        Console.WriteLine("     |     |     ");
        Console.WriteLine($"  {board[3]}  |  {board[4]}  |  {board[5]} ");
        Console.WriteLine("_____|_____|_____");
        Console.WriteLine("     |     |     ");
        Console.WriteLine($"  {board[6]}  |  {board[7]}  |  {board[8]} ");
        Console.WriteLine("     |     |     ");
    }

    static int GetPlayerChoice()
    {
        int choice;
        Console.Write($"Игрок {currentPlayer}, выберите номер ячейки: ");
        while (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > 9 || board[choice - 1] == 'X' || board[choice - 1] == 'O')
        {
            Console.Write("Неверный выбор. Попробуйте снова: ");
        }
        return choice;
    }

    static void MakeMove(int choice)
    {
        board[choice - 1] = currentPlayer == 1 ? 'X' : 'O';
    }

    static int CheckWin()
    {
        int[,] winCombinations = new int[,]
        {
            {0, 1, 2}, {3, 4, 5}, {6, 7, 8}, // горизонтально
            {0, 3, 6}, {1, 4, 7}, {2, 5, 8}, // вертикально
            {0, 4, 8}, {2, 4, 6}             // диагонально
        };

        for (int i = 0; i < winCombinations.GetLength(0); i++)
        {
            if (board[winCombinations[i, 0]] == board[winCombinations[i, 1]] && board[winCombinations[i, 1]] == board[winCombinations[i, 2]])
            {
                return 1; // Победа
            }
        }

        if (Array.TrueForAll(board, c => c == 'X' || c == 'O'))
        {
            return -1; // Ничья
        }

        return 0; // Игра продолжается
    }

}