sing System;

class Program
{
    static int[,] board = new int[4, 4];
    static int emptyX = 3, emptyY = 3; // Начальная позиция пустой ячейки

    static void Main()
    {
        InitializeBoard();
        ShuffleBoard();

        while (true)
        {
            Console.Clear();
            DisplayBoard();

            if (CheckWin())
            {
                Console.WriteLine("Поздравляем! Вы выиграли!");
                break;
            }

            ConsoleKey key = Console.ReadKey().Key;
            switch (key)
            {
                case ConsoleKey.W: Move(1, 0); break; // Вверх
                case ConsoleKey.S: Move(-1, 0); break; // Вниз
                case ConsoleKey.A: Move(0, 1); break; // Влево
                case ConsoleKey.D: Move(0, -1); break; // Вправо
            }
        }
    }

    static void InitializeBoard()
    {
        int value = 1;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                board[i, j] = value++;
            }
        }
        board[3, 3] = 0; // Пустая ячейка
    }

    static void ShuffleBoard()
    {
        Random random = new Random();
        for (int i = 0; i < 100; i++)
        {
            switch (random.Next(4))
            {
                case 0: Move(1, 0); break; // Вверх
                case 1: Move(-1, 0); break; // Вниз
                case 2: Move(0, 1); break; // Влево
                case 3: Move(0, -1); break; // Вправо
            }
        }
    }

    static void DisplayBoard()
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (board[i, j] == 0)
                {
                    Console.Write("   ");
                }
                else
                {
                    Console.Write($"{board[i, j],2} ");
                }
            }
            Console.WriteLine();
        }
    }

    static void Move(int dx, int dy)
    {
        int newX = emptyX + dx;
        int newY = emptyY + dy;

        if (newX >= 0 && newX < 4 && newY >= 0 && newY < 4)
        {
            board[emptyX, emptyY] = board[newX, newY];
            board[newX, newY] = 0;
            emptyX = newX;
            emptyY = newY;
        }
    }

    static bool CheckWin()
    {
        int value = 1;
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (board[i, j] != value % 16)
                {
                    return false;
                }
                value++;
            }
        }
        return true;
    }
}