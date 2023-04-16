DependentTasks.Make(args);

public static class DependentTasks
{
    /// <summary>
    ///     Заполняет массив строк текстом из файла
    /// </summary>
    /// <param name="fileName">Имя файла из которого происходит заполнение</param>
    /// <returns>Массив строк из текста файла</returns>
    private static string[] FillTheArrayOfTasks(string fileName)
    {
        var lines = File.ReadAllLines(fileName);
        return lines;
    }

    /// <summary>
    ///     Выполняет эмуляцию сборки, под данным из файла указанном в константе fileName метода, в котором указан список задач
    ///     и действия,
    ///     в метод передается название задачи, действия которой выполняются, после того как выполнятся её зависимые задачи.
    ///     Результат работы выводится в консоль.
    /// </summary>
    /// <param name="args">Массив, в котором должен быть один элемент, представляющий собой название целевой задачи</param>
    internal static void Make(string[] args)
    {
        const string fileName = "makefile.txt";

        switch (args.Length)
        {
            case < 1:
                Console.WriteLine("Не указана задача для выполнения. Повторите пожалуйста запрос");
                return;
            case > 1:
                Console.WriteLine("Не корректно указана задача для выполнения. Повторите пожалуйста запрос");
                return;
        }

        var mainTarget = args[0];

        var lines = FillTheArrayOfTasks(fileName);
        ParseTheArrayOfTaskAsDictionaries(lines, out var tasksDependencies, out var tasksActions);

        var path = FindPath(mainTarget, tasksDependencies);
        while (path.Count > 0)
        {
            Console.WriteLine(path.Peek());
            foreach (var action in tasksActions[path.Pop()]) Console.WriteLine(action);
        }
    }

    /// <summary>
    ///     Разбирает массив строк на словарь зависимостей и словарь действий для всех задач
    /// </summary>
    /// <param name="lines">Массив строк для разбора</param>
    /// <param name="tasksDependencies">
    ///     Словарь, пары ключ-значение которого соответствуют паре: имя задачи - массив имен
    ///     зависимостей этой задачи
    /// </param>
    /// <param name="tasksActions">
    ///     Словарь, пары ключ-значение которого соответствуют паре: имя задачи - список действий в этой
    ///     задаче
    /// </param>
    private static void ParseTheArrayOfTaskAsDictionaries(string[] lines,
        out Dictionary<string, string[]> tasksDependencies, out Dictionary<string, List<string>> tasksActions)
    {
        tasksDependencies = new Dictionary<string, string[]>();
        tasksActions = new Dictionary<string, List<string>>();
        if (lines.Length == 0) Console.WriteLine("Файл пуст, пожалуйста, заполните его.");
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
                throw new ArgumentException(
                    $"Найдена пустая строка, пожалуйста, проверьте содержимое файла. Номер строки: {i}.", nameof(line));

            if (StartsWithWhitespace(line, i))
                throw new ArgumentException(
                    "У этой строки не должно быть пробелов или символов табуляции в начале. " +
                    $"Пожалуйста, проверьте содержимое файла. Номер строки: {i}.", nameof(line));


            // Заполнение списка зависимостей для текущей задачи
            var separatedLine = line.Split(new[] { " " }, 2, StringSplitOptions.RemoveEmptyEntries);
            string[] listOfTasksInDependencies = null;
            if (separatedLine.Length > 1)
                listOfTasksInDependencies =
                    separatedLine[1].Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            var currentTask = separatedLine[0];

            // Проверка есть ли зависимости для корректного запоминания имени задачи. Учет того, что название может заканчиваться на ":".
            if (currentTask.EndsWith(':'))
            {
                if (listOfTasksInDependencies != null)
                    currentTask = currentTask.Remove(currentTask.Length - 1);
                else
                    throw new ArgumentException(
                        $"В строке у задачи нет зависимостей, но стоит символ \":\", проверьте содержимое файла. Номер строки: {i}.",
                        nameof(line));
            }
            else
            {
                if (listOfTasksInDependencies != null)
                    throw new ArgumentException(
                        $"В строке у задачи найдены зависимости, но не стоит символ \":\", проверьте содержимое файла. Номер строки: {i}.",
                        nameof(line));
            }

            tasksDependencies.Add(currentTask, listOfTasksInDependencies);

            // Заполнение списка действий для текущей задачи
            var listOfActions = new List<string>();
            while (i + 1 < lines.Length && StartsWithWhitespace(lines[i + 1], i + 1))
            {
                listOfActions.Add(lines[i + 1]);
                i++;
            }

            tasksActions.Add(currentTask, listOfActions);
        }
    }

    /// <summary>
    ///     Поиск пути для выполнения задачи, путем итеративного прохода по зависимостям
    /// </summary>
    /// <param name="mainTarget">Задача которая требует выполнения</param>
    /// <param name="tasksDependencies">
    ///     Словарь, пары ключ-значение которого соответствуют паре: имя задачи - массив имен
    ///     зависимостей этой задачи
    /// </param>
    /// <returns>Стек с задачами для выполнения, соответствующий пути выполнения зависимых задач</returns>
    private static Stack<string> FindPath(string mainTarget, Dictionary<string, string[]> tasksDependencies)
    {
        var path = new Stack<string>();
        var stackOfIteration = new Stack<string>();

        if (!tasksDependencies.ContainsKey(mainTarget))
            throw new KeyNotFoundException(
                $"Не найдена задача с запрошенным названием: \"{mainTarget}\", пожалуйста, проверьте входной параметр.");

        // Поиск пути для выполнения задач по алгоритму прохождения направленного графа в глубину итеративным методом.
        var currentTarget = mainTarget;
        stackOfIteration.Push(mainTarget);
        path.Push(currentTarget);


        while (stackOfIteration.Count > 0)
        {
            currentTarget = stackOfIteration.Peek();
            string nextNode = null;
            try
            {
                if (tasksDependencies[currentTarget] != null)
                    // Поиск первой попавшейся зависимой задачи, которая ещё не содержится в стеке пройденного пути - path
                    foreach (var n in tasksDependencies[currentTarget])
                    {
                        if (!path.Contains(n))
                        {
                            nextNode = n;
                            break;
                        }

                        // Проверка на цикл
                        if (stackOfIteration.Contains(n))
                            throw new Exception("Невозможно определить порядок выполнения задач из-за цикличности");
                    }

                // Добавление зависимой задачи в стек пройденного пути и в стек итерации если удалось найти не пройденную задачу
                if (nextNode != null)
                {
                    path.Push(nextNode);
                    stackOfIteration.Push(nextNode);
                }
                // Иначе происходит проверка является текущая задача - задачей требующей выполнения
                // Если да то метод возвращает полный путь, если нет то возврат к проверке зависимой задачи
                else
                {
                    if (currentTarget == mainTarget) return path;
                    stackOfIteration.Pop();
                }
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine(
                    $"У задачи найдена зависимость: \"{currentTarget}\" которая не указывает ни на одну задачу");
                throw;
            }
        }
        
        // Недостижимый код
        return path;
    }

    /// <summary>
    ///     Проверка начинается ли строка с символа табуляции или пробела
    /// </summary>
    /// <param name="line">Проверяемая строка</param>
    /// <param name="i">Номер строки из файла</param>
    /// <returns>true если строка начинается с символа табуляции или пробела</returns>
    /// <exception cref="ArgumentException">Исключение выбрасываемое если строка оказывается "белым пространством" или пустой</exception>
    private static bool StartsWithWhitespace(string line, int i)
    {
        if (string.IsNullOrWhiteSpace(line))
            throw new ArgumentException(
                $"Найдена пустая строка, пожалуйста, проверьте содержимое файла. Номер строки: {i}", nameof(line));
        return line[0] == ' ' || line[0] == '\t';
    }
}