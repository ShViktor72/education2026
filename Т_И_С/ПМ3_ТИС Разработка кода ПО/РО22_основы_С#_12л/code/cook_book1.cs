           Directory.SetCurrentDirectory("C:\\Users\\User\\Desktop");
           
           string filePath = "recipes.txt"; 

           // объявляем словарь, ключ - строка, значение - любой тип
           var cookBook = new Dictionary<string, List<Dictionary<string, object>>>();

           // считываем файл в массив строк
           var lines = File.ReadAllLines(filePath);
           int i = 0;

           while (i < lines.Length)
           {
               string dishName = lines[i]; // Название блюда
               i++;

               // если строка пустая - пропускаем
               if (string.IsNullOrEmpty(dishName))
                   continue;

               int ingredientCount = int.Parse(lines[i]); // Количество ингредиентов
               i++;

               // словарь ингридиентов
               var ingredients = new List<Dictionary<string, object>>();

               for (int j = 0; j < ingredientCount; j++)
               {
                   var ingredientParts = lines[i].Split("|");
                   i++;


                   var ingredient = new Dictionary<string, object>
                   {
                       {"ingredient_name", ingredientParts[0]},
                       {"quantity", int.Parse(ingredientParts[1])},
                       {"measure", ingredientParts[2]}
                   };
                   ingredients.Add(ingredient);

               }

               cookBook[dishName] = ingredients;
           }

           // Вывод результата
           foreach (var dish in cookBook)
           {
               Console.WriteLine($"{dish.Key}:");
               foreach (var ingredient in dish.Value)
               {
                   Console.WriteLine($"{ingredient["ingredient_name"]} * {ingredient["quantity"]} * {ingredient["measure"]}");
               }
               Console.WriteLine();
           }