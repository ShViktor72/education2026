// Вариант №1: Приложение «Архиватор-загрузчик»
1. Файл Program.cs
Здесь настраивается Глобальная обработка. Если студент забудет try-catch, это событие спасет программу от вылета.

C#

using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ArchiveLoader
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // ЗАДАНИЕ 2: Глобальный перехват ошибок в UI-потоке
            Application.ThreadException += new ThreadExceptionEventHandler(GlobalExceptionHandler);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.Run(new MainForm());
        }

        static void GlobalExceptionHandler(object sender, ThreadExceptionEventArgs e)
        {
            // ЗАДАНИЕ 3: Логирование глобальной ошибки
            string errorMessage = $"[ГЛОБАЛЬНАЯ ОШИБКА] {e.Exception.GetType().Name}: {e.Exception.Message}";
            WriteToLog(errorMessage);

            MessageBox.Show("Произошла критическая ошибка, но приложение продолжает работу. Детали в session.log", 
                            "Глобальный перехват", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // Вспомогательный метод для записи в файл
        public static void WriteToLog(string message)
        {
            try
            {
                string logLine = $"[{DateTime.Now:dd.MM.yyyy HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText("session.log", logLine);
            }
            catch { /* Если не удалось записать в лог, ничего не делаем, чтобы не зациклиться */ }
        }
    }
}
2. Файл MainForm.cs
Основная логика приложения. На форме должны быть: TextBox (для пути к файлу), Button (Прочитать), Button (Секретная ошибка).

C#

using System;
using System.IO;
using System.Windows.Forms;

namespace ArchiveLoader
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            // ЗАДАНИЕ 3: Логируем запуск приложения
            Program.WriteToLog("Приложение запущено.");
        }

        // ЗАДАНИЕ 1: Локальная защита (Try-Catch)
        private void btnReadFile_Click(object sender, EventArgs e)
        {
            string filePath = txtFilePath.Text;

            try
            {
                // Попытка прочитать файл
                string content = File.ReadAllText(filePath);
                MessageBox.Show("Файл успешно прочитан!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Program.WriteToLog($"Успешно прочитан файл: {filePath}");
            }
            catch (FileNotFoundException ex)
            {
                // Специфическая ошибка: файл не найден
                string msg = $"Файл не найден по пути: {filePath}";
                Program.WriteToLog($"[ОШИБКА] {msg}");
                MessageBox.Show(msg, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                // Общая ошибка (например, нет прав доступа)
                Program.WriteToLog($"[ОШИБКА] {ex.Message}");
                MessageBox.Show($"Не удалось прочитать файл: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ЗАДАНИЕ 2: Намеренная ошибка без try-catch
        private void btnSecretError_Click(object sender, EventArgs e)
        {
            // Специально вызываем NullReferenceException
            string s = null;
            int length = s.Length; // Здесь программа должна была бы упасть
        }
    }
}
Как это работает (для студента):
Запуск: В файле session.log появляется строка о запуске.

Тест 1 (Локальный): Если ввести в текстовое поле C:\non_existent.txt и нажать кнопку, сработает catch (FileNotFoundException). В лог запишется ошибка, программа выдаст предупреждение.

Тест 2 (Глобальный): Нажмите кнопку «Секретная ошибка». Поскольку там нет try-catch, управление передастся в Program.cs. Выскочит сообщение «Глобальный перехват», а в лог запишется NullReferenceException. Программа при этом не закроется.

Просмотр лога: Файл session.log появится в папке bin/Debug вашего проекта.
	
	
	
Варианта №2: Приложение «Математический анализатор». Здесь основной упор сделан на работу с числами, типами данных и предотвращение арифметических сбоев.

1. Файл Program.cs (Глобальная страховка)
Этот код идентичен для обоих вариантов, так как он защищает всё приложение целиком.

C#

using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace MathAnalyzer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // ЗАДАНИЕ 2: Глобальный перехват ошибок
            Application.ThreadException += new ThreadExceptionEventHandler(GlobalHandler);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.Run(new MainForm());
        }

        static void GlobalHandler(object sender, ThreadExceptionEventArgs e)
        {
            // ЗАДАНИЕ 3: Запись критического сбоя в лог
            string logEntry = $"[{DateTime.Now}] [CRITICAL] {e.Exception.GetType().Name}: {e.Exception.Message}\n";
            File.AppendAllText("errors.txt", logEntry);

            MessageBox.Show("Произошел критический сбой. Детали записаны в errors.txt", 
                            "Математический анализатор", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }
    }
}
2. Файл MainForm.cs (Математическая логика)
На форме: TextBox для ввода числа, Button («Вычислить результат»), Button («Вызвать сбой массива»).

C#

using System;
using System.IO;
using System.Windows.Forms;

namespace MathAnalyzer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        // ЗАДАНИЕ 3: Метод для логирования
        private void WriteLog(string type, string message)
        {
            // Формат: [Дата Время] [Тип] Сообщение
            string line = $"[{DateTime.Now:G}] [{type}] {message}{Environment.NewLine}";
            File.AppendAllText("errors.txt", line);
        }

        // ЗАДАНИЕ 1: Локальная защита (Try-Catch)
        private void btnCalculate_Click(object sender, EventArgs e)
        {
            try
            {
                // Попытка преобразовать текст в число
                int divisor = int.Parse(txtInput.Text);

                // Попытка деления
                int result = 1000 / divisor;

                MessageBox.Show($"1000 разделить на {divisor} будет {result}", "Результат");
                
                // Логируем успех
                WriteLog("INFO", $"Успешный расчет. Делитель: {divisor}");
            }
            catch (FormatException)
            {
                // Ошибка: ввели буквы
                string msg = "Ошибка: введите целое число цифрами!";
                WriteLog("FORMAT_ERROR", msg);
                MessageBox.Show(msg, "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (DivideByZeroException)
            {
                // Ошибка: деление на ноль
                string msg = "Ошибка: нельзя делить на ноль!";
                WriteLog("MATH_ERROR", msg);
                MessageBox.Show(msg, "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                // Любая другая ошибка
                WriteLog("UNKNOWN_ERROR", ex.Message);
                MessageBox.Show("Что-то пошло не так: " + ex.Message);
            }
        }

        // ЗАДАНИЕ 2: Глобальная ошибка (выход за границы массива)
        private void btnArrayCrash_Click(object sender, EventArgs e)
        {
            // Намеренная ошибка без try-catch
            int[] numbers = { 1, 2, 3 };
            int crash = numbers[99]; // Индекса 99 не существует
        }
    }
}
Инструкция по проверке для студента:
Проверка логирования: При первом запуске и ошибке файл errors.txt должен появиться в папке bin/Debug вашего проекта.

Проверка FormatException: Введите в поле слово «привет». Нажмите вычислить. В логе должна появиться запись [FORMAT_ERROR].

Проверка DivideByZero: Введите цифру 0. Проверьте, что сработал именно этот блок catch.

Проверка Глобального перехвата: Нажмите вторую кнопку. Программа не должна закрыться. Вместо этого сработает метод GlobalHandler из Program.cs. Проверьте, что в логе появилась запись со словом [CRITICAL].	