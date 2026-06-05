using System.Globalization;

namespace VedomostPokupnikh
{
    /// <summary>
    /// Формирует ведомость покупных изделий на основе данных Loodsman Web API.
    /// </summary>
    public sealed class PurchaseReportBuilder
    {
        private const string LinkConsistsOf = "Состоит из ...";
        private const string LinkRepresents = "Представляет собой";
        private const string LinkDocuments = "Документы";
        private const string LinkMadeFrom = "Изготавливается из ...";

        private const string TypeComplex = "Комплекс";
        private const string TypeComplect = "Комплект";
        private const string TypeAssembly = "Сборочная единица";
        private const string TypeSpecifiedProduct = "Специфицированное изделие";
        private const string TypeRepresentation = "Представление";
        private const string TypeAdditionalSection = "Дополнительный раздел";
        private const string TypeSelectionElement = "Подборный элемент";
        private const string TypeDetail = "Деталь";
        private const string TypeOtherProduct = "Прочее изделие";
        private const string TypeOtherFromReference = "Прочее из справочника";
        private const string TypeStandardProduct = "Стандартное изделие";
        private const string TypePurchaseReport = "Ведомость покупных изделий";
        private const string TypeSpecification = "Спецификация";

        private readonly LoodsmanApiClient _apiClient;
        private readonly Dictionary<int, LoodsmanVersionInfo> _versions = new();
        private readonly Dictionary<int, Dictionary<string, string?>> _attributes = new();
        private readonly Dictionary<(int IdVersion, string LinkType, bool Inverse), List<LoodsmanLinkedObject>> _links = new();
        private readonly Dictionary<int, Dictionary<string, string?>> _linkAttributes = new();

        /// <summary>
        /// Создаёт построитель отчёта.
        /// </summary>
        /// <param name="apiClient">Клиент для чтения данных из Loodsman Web API.</param>
        public PurchaseReportBuilder(LoodsmanApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        /// <summary>
        /// Строит итоговые строки отчёта для корневого изделия.
        /// </summary>
        /// <param name="rootVersionId">Идентификатор версии корневого изделия.</param>
        /// <returns>Список строк отчёта в формате, ожидаемом шаблоном.</returns>
        public async Task<List<Dictionary<string, object?>>> Build(int rootVersionId)
        {
            // Вызовы метаданных сразу проверяют доступность API v4, сессию и выбранную базу.
            await _apiClient.GetTypes();
            await _apiClient.GetLinkTypes();

            var root = await GetVersion(rootVersionId);
            var rootAttributes = await GetAttributes(rootVersionId);
            var topName = GetAttr(rootAttributes, "Наименование");
            var topProduct = root.Product;

            // Корневое изделие добавляется как первая строка состава.
            // Дальше все количества будут накапливаться относительно этой строки.
            var composition = new List<CompositionRow>();
            var rootRow = new CompositionRow
            {
                Nn = 1,
                IdChild = rootVersionId,
                IdChild2 = IsTransparentType(root.Type) ? null : rootVersionId,
                IdRoot = rootVersionId,
                Type = root.Type,
                Product = root.Product,
                Qty = 1,
                QtyAll = 1,
                Unit = string.Empty,
                IsPokup = IsPurchased(root.Type, rootAttributes)
            };

            composition.Add(rootRow);

            // Сначала полностью раскрываем дерево состава, а уже потом применяем правила отчёта.
            // Это позволяет принимать решения по фильтрации уже с учётом всей вложенной структуры.
            await ExpandComposition(rootRow, composition, new HashSet<int>());

            // После раскрытия состава последовательно применяются бизнес-правила отчёта:
            // принадлежность к комплектам, количество на регулировку и исключение лишних веток.
            await FillAccessories(composition);
            await FillRegulationQuantities(composition);
            var deleteEntries = await BuildDeleteEntries(composition, rootVersionId);
            var filtered = FilterComposition(composition, deleteEntries, rootVersionId);
            var resultRows = await BuildIntermediateRows(filtered, deleteEntries, rootVersionId);
            return BuildFinalRows(resultRows, topProduct, topName);
        }

        /// <summary>
        /// Рекурсивно раскрывает состав изделия по связям состава и представления.
        /// </summary>
        /// <param name="parent">Родительская строка состава.</param>
        /// <param name="rows">Накопленный список строк состава.</param>
        /// <param name="path">Текущий путь обхода для защиты от циклов.</param>
        private async Task ExpandComposition(CompositionRow parent, List<CompositionRow> rows, HashSet<int> path)
        {
            // Раскрывать нужно только те типы, которые в PLM могут содержать значимый состав.
            // path защищает от случайных циклических связей в данных.
            if (!CanExpand(parent.Type) || !path.Add(parent.IdChild))
                return;

            // Для отчёта обычный состав и связь "Представляет собой"
            // обрабатываются одинаково и попадают в единое дерево.
            var children = new List<LoodsmanLinkedObject>();
            children.AddRange(await GetLinked(parent.IdChild, LinkConsistsOf));
            children.AddRange(await GetLinked(parent.IdChild, LinkRepresents));

            foreach (var child in children)
            {
                var attrs = await GetAttributes(child.IdVersion);
                var childType = child.Type;

                // Всё, что находится ниже комплекта, должно попасть в колонку
                // "Количество/в комплекты", а не "Количество/на изделие".
                var inComplect = childType == TypeComplect ? true : parent.InComplect;

                // Для комплектов запоминаем отдельный корень ветки, чтобы потом
                // корректно распространить принадлежность к спецификации комплекта.
                var idRoot = childType == TypeComplect ? child.IdVersion : parent.IdRoot;

                var row = new CompositionRow
                {
                    Nn = rows.Count + 1,
                    IdParent = parent.IdChild,
                    IdParent2 = parent.IdChild2,
                    IdChild = child.IdVersion,

                    // Подборный элемент и представление являются техническими узлами:
                    // они не должны становиться смысловым родителем покупной позиции.
                    IdChild2 = IsTransparentType(childType) ? parent.IdChild2 : child.IdVersion,
                    IdRoot = idRoot,
                    IdLink = child.IdLink,
                    Type = childType,
                    Product = child.Product,
                    Qty = child.MinQuantity ?? 1,

                    // Накопленное количество получается умножением количества текущей связи
                    // на количество всех родительских уровней.
                    QtyAll = (child.MinQuantity ?? 1) * parent.QtyAll,
                    Unit = child.Unit ?? string.Empty,
                    InComplect = inComplect,
                    IsPokup = IsPurchased(childType, attrs)
                };

                rows.Add(row);
                await ExpandComposition(row, rows, path);
            }

            path.Remove(parent.IdChild);
        }

        /// <summary>
        /// Заполняет принадлежность к спецификации для элементов, входящих в комплекты.
        /// </summary>
        /// <param name="rows">Строки раскрытого состава.</param>
        private async Task FillAccessories(List<CompositionRow> rows)
        {
            // Если внутри комплекта найден документ-спецификация, его обозначение
            // становится признаком принадлежности для всей ветки комплекта.
            foreach (var row in rows.Where(r => r.IdParent.HasValue && r.InComplect))
            {
                if ((await GetLinked(row.IdChild, LinkDocuments)).Any(d => d.Type == TypeSpecification))
                    row.Accessory = row.Product;
            }

            // Найденная спецификация комплекта применяется ко всем дочерним позициям
            // внутри той же ветки комплекта.
            foreach (var source in rows.Where(r => !string.IsNullOrWhiteSpace(r.Accessory)).ToList())
            {
                foreach (var row in rows.Where(r => r.InComplect && r.IdRoot == source.IdRoot))
                {
                    if (IsDescendant(rows, source, row) && string.IsNullOrWhiteSpace(row.Accessory))
                        row.Accessory = source.Accessory;
                }
            }
        }

        /// <summary>
        /// Рассчитывает количество на регулировку по атрибутам связей состава.
        /// </summary>
        /// <param name="rows">Строки раскрытого состава.</param>
        private async Task FillRegulationQuantities(List<CompositionRow> rows)
        {
            foreach (var row in rows.Where(r => r.IdLink.HasValue))
            {
                var attrs = await GetLinkAttributes(row.IdLink!.Value);
                var qty = GetAttr(attrs, "Количество на регулировку");

                // В базе регулировка хранится на связи состава, поэтому её нужно
                // умножить на уже накопленное количество текущей позиции.
                if (TryParseDouble(qty, out var value))
                    row.QtyRegulirovka = value * row.QtyAll;
            }
        }

        /// <summary>
        /// Формирует правила удаления и замены строк состава перед построением отчёта.
        /// </summary>
        /// <param name="rows">Строки раскрытого состава.</param>
        /// <param name="rootVersionId">Идентификатор версии корневого изделия.</param>
        /// <returns>Словарь правил удаления или замены по идентификатору ДСЕ.</returns>
        private async Task<Dictionary<int, DeleteEntry>> BuildDeleteEntries(List<CompositionRow> rows, int rootVersionId)
        {
            var entries = new Dictionary<int, DeleteEntry>();

            // Если у ДСЕ есть собственная "Ведомость покупных изделий",
            // её состав не раскрывается в текущей ведомости, а сама ДСЕ
            // выводится отдельной строкой-разделом с признаком исключения.
            foreach (var row in rows.Where(r => IsSpecifiedType(r.Type)))
            {
                var report = await GetDocumentOfType(row.IdChild, TypePurchaseReport);
                if (report != null || row.IdChild == rootVersionId)
                {
                    entries[row.IdChild] = new DeleteEntry(row.IdChild, false, null);
                }
            }

            // Для деталей проверяется связь "Изготавливается из ...".
            // Если деталь изготавливается из покупной заготовки, в отчёт должна попасть
            // именно заготовка, а не исходная деталь.
            foreach (var row in rows.Where(r => r.Type == TypeDetail))
            {
                var materials = await GetLinked(row.IdChild, LinkMadeFrom);
                foreach (var material in materials)
                {
                    if (!IsPurchaseMaterialType(material.Type))
                        continue;

                    var attrs = await GetAttributes(material.IdVersion);
                    if (IsPurchased(material.Type, attrs))
                    {
                        entries[row.IdChild] = new DeleteEntry(row.IdChild, false, material.IdVersion);
                        break;
                    }
                }
            }

            return entries;
        }

        /// <summary>
        /// Удаляет из состава непокупные позиции и ветки, которые закрываются отдельной ведомостью.
        /// </summary>
        /// <param name="rows">Исходные строки состава.</param>
        /// <param name="deleteEntries">Правила удаления и замены строк состава.</param>
        /// <param name="rootVersionId">Идентификатор версии корневого изделия.</param>
        /// <returns>Отфильтрованные строки состава.</returns>
        private static List<CompositionRow> FilterComposition(
            List<CompositionRow> rows,
            Dictionary<int, DeleteEntry> deleteEntries,
            int rootVersionId)
        {
            // deleteRoots содержит ДСЕ, чьи дочерние ветки нужно убрать:
            // покупные изделия из такой ветки будут отражены в собственной ведомости этой ДСЕ.
            var deleteRoots = deleteEntries.Values
                .Where(e => !e.DelSelf && e.Id != rootVersionId)
                .Select(e => e.Id)
                .ToHashSet();
            var descendantRows = new HashSet<int>();
            var changed = true;

            // Собираем всех потомков удаляемых корней. Цикл нужен, потому что
            // строки состава лежат плоским списком, а глубина дерева заранее неизвестна.
            while (changed)
            {
                changed = false;
                foreach (var row in rows)
                {
                    if (!row.IdParent.HasValue)
                        continue;

                    if (deleteRoots.Contains(row.IdParent.Value) ||
                        rows.Any(parent => parent.Nn == row.Nn && descendantRows.Contains(parent.Nn)) ||
                        rows.Any(parent => parent.IdChild == row.IdParent.Value && descendantRows.Contains(parent.Nn)))
                    {
                        changed |= descendantRows.Add(row.Nn);
                    }
                }
            }

            // В отчёте остаются только покупные позиции и специальные строки,
            // которые нужны для раздела "Ведомости покупных изделий составных частей".
            return rows
                .Where(row => !row.IdParent.HasValue || !deleteRoots.Contains(row.IdParent.Value))
                .Where(row => !descendantRows.Contains(row.Nn))
                .Where(row => row.IsPokup || deleteEntries.ContainsKey(row.IdChild))
                .ToList();
        }

        /// <summary>
        /// Преобразует отфильтрованный состав во внутренние строки отчёта с атрибутами и количествами.
        /// </summary>
        /// <param name="rows">Отфильтрованные строки состава.</param>
        /// <param name="deleteEntries">Правила удаления и замены строк состава.</param>
        /// <param name="rootVersionId">Идентификатор версии корневого изделия.</param>
        /// <returns>Список внутренних строк отчёта.</returns>
        private async Task<List<IntermediateReportRow>> BuildIntermediateRows(
            List<CompositionRow> rows,
            Dictionary<int, DeleteEntry> deleteEntries,
            int rootVersionId)
        {
            // Группировка выполняется по объекту, родителю, единице измерения
            // и принадлежности к спецификации. Если есть покупная заготовка,
            // ключом результата становится заготовка, но связь с исходной деталью сохраняется.
            var grouped = rows
                .GroupBy(row =>
                {
                    deleteEntries.TryGetValue(row.IdChild, out var entry);
                    return new
                    {
                        ResultId = entry?.IdZagotovka ?? row.IdChild,
                        row.IdChild,
                        row.IdParent,
                        row.IdParent2,
                        row.Type,
                        row.Unit,
                        row.Accessory,
                        IdZagotovka = entry?.IdZagotovka
                    };
                });

            var result = new List<IntermediateReportRow>();

            foreach (var group in grouped)
            {
                var key = group.Key;
                var version = await GetVersion(key.ResultId);
                var attrs = await GetAttributes(key.ResultId);
                var parentVersion = key.IdParent2.HasValue ? await GetVersion(key.IdParent2.Value) : null;
                var excludeDocument = await GetDocumentOfType(key.IdChild, TypePurchaseReport);

                var name = GetAttr(attrs, "Наименование");
                var standard = GetAttr(attrs, "Обозначение стандарта");
                var exclude = excludeDocument != null ? 1 : 0;
                var product = version.Product;

                // Правила формирования наименования зависят от типа позиции:
                // заготовки получают пояснение, стандартные изделия очищаются от стандарта,
                // а детали и специфицированные изделия получают вид "Обозначение-Наименование".
                if (key.IdZagotovka.HasValue)
                {
                    name = $"{name ?? string.Empty} (Заготовка для {product})";
                }
                else if (IsCatalogPurchaseType(key.Type))
                {
                    name = string.IsNullOrEmpty(standard)
                        ? product
                        : (product ?? string.Empty).Replace(standard, string.Empty);
                }
                else if (IsSpecifiedType(key.Type) || key.Type == TypeDetail)
                {
                    name = name is null ? product : $"{product}-{name}";
                }

                // Для исключённых строк документом на поставку становится не product ДСЕ,
                // а обозначение найденной ведомости покупных изделий.
                var document = key.Type switch
                {
                    TypeOtherProduct or TypeOtherFromReference or TypeStandardProduct => standard,
                    TypeDetail => product,
                    _ when exclude == 1 => string.Empty,
                    _ => product
                };

                if (exclude == 1)
                    document = excludeDocument?.Product;

                // "Куда входит" берётся из спецификации комплекта, если она найдена.
                // Иначе для непосредственных дочерних элементов корня выводится пустая строка,
                // а для более глубоких элементов - обозначение смыслового родителя.
                var parent = !string.IsNullOrWhiteSpace(key.Accessory)
                    ? key.Accessory
                    : key.IdParent2 == rootVersionId
                        ? string.Empty
                        : parentVersion?.Product;

                result.Add(new IntermediateReportRow
                {
                    IdDse = key.ResultId,
                    IdChild = key.IdChild,
                    Type = key.Type,
                    Name = name,
                    Product = product,
                    Vid = GetAttr(attrs, "Вид изделия"),
                    DocumentOboznachenie = document,
                    Tiporazmer = GetAttr(attrs, "Типоразмер"),
                    Parent = parent,
                    CodProdukcii = GetAttr(attrs, "Код ОКП / по классификатору"),
                    Postavshik = GetAttr(attrs, "Поставщик"),

                    // Количество на регулировку вычитается из "на изделие"/"в комплекты",
                    // потому что в итоговой форме оно выводится отдельной колонкой.
                    QtyIzd = group.Sum(r => r.InComplect ? 0 : r.QtyAll - (r.QtyRegulirovka ?? 0)),
                    QtyKomplekty = group.Sum(r => r.InComplect ? r.QtyAll - (r.QtyRegulirovka ?? 0) : 0),
                    QtyRegulirovka = group.Sum(r => r.QtyRegulirovka ?? 0),
                    QtyAll = group.Sum(r => r.QtyAll),
                    Unit = key.Unit,
                    Exclude = exclude,
                    OboznStand = standard
                });
            }

            return result;
        }

        /// <summary>
        /// Формирует финальные строки отчёта с русскими названиями колонок.
        /// </summary>
        /// <param name="rows">Внутренние строки отчёта.</param>
        /// <param name="topProduct">Обозначение корневого изделия.</param>
        /// <param name="topName">Наименование корневого изделия.</param>
        /// <returns>Строки отчёта в формате итогового JSON.</returns>
        private static List<Dictionary<string, object?>> BuildFinalRows(
            List<IntermediateReportRow> rows,
            string? topProduct,
            string? topName)
        {
            // "Количество итого" считается по одинаковым наименованию и документу поставки,
            // чтобы одинаковые покупные изделия показывали общий итог по всему изделию.
            var totals = rows
                .Where(row => row.Exclude == 0 && !string.IsNullOrWhiteSpace(row.Name) && row.DocumentOboznachenie != null)
                .GroupBy(row => (row.Name, row.DocumentOboznachenie))
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(row => row.QtyIzd + row.QtyKomplekty + row.QtyRegulirovka));

            return rows
                .GroupBy(row => new
                {
                    row.Name,
                    row.Exclude,
                    row.IdDse,
                    row.Unit,
                    row.Vid,
                    row.DocumentOboznachenie,
                    row.Tiporazmer,
                    row.CodProdukcii,
                    row.Postavshik,
                    row.Parent,
                    row.OboznStand
                })
                .Select(group =>
                {
                    var row = group.First();
                    totals.TryGetValue((row.Name, row.DocumentOboznachenie), out var total);

                    // Пустые значения намеренно сериализуются как {}, потому что
                    // шаблон отчёта использует такой объект как признак незаполненной ячейки.
                    return new Dictionary<string, object?>
                    {
                        ["Наименование"] = Value(row.Name),
                        ["Вид Изделия"] = row.Exclude == 1 ? "Ведомости покупных изделий составных частей" : Value(row.Vid),
                        ["Обозначение док-та на поставку"] = Value(row.DocumentOboznachenie),
                        ["Типоразмер"] = Value(row.Tiporazmer),
                        ["Куда входит"] = row.Parent is null ? Empty() : row.Parent,
                        ["Код продукции"] = Value(row.CodProdukcii),
                        ["Поставщик"] = Value(row.Postavshik),
                        ["Количество/на изделие"] = row.Exclude == 1 ? Empty() : Number(Math.Round(group.Sum(r => r.QtyIzd), 2)),
                        ["Количество/в комплекты"] = row.Exclude == 1 ? Empty() : Number(Math.Round(group.Sum(r => r.QtyKomplekty), 2)),
                        ["Количество/на регулировку"] = row.Exclude == 1 ? Empty() : Number(group.Sum(r => r.QtyRegulirovka)),
                        ["Количество всего"] = row.Exclude == 1 ? Empty() : Number(group.Sum(r => r.QtyIzd + r.QtyKomplekty + r.QtyRegulirovka)),
                        ["Количество итого"] = row.Exclude == 1 || total == 0 ? Empty() : Number(Math.Round(total, 2)),
                        ["ЕИ количества"] = Value(row.Unit),
                        ["ЕИ количества1"] = Value(row.Unit),
                        ["ЕИ количества2"] = Value(row.Unit),
                        ["ЕИ количества3"] = Value(row.Unit),
                        ["ЕИ количества4"] = Value(row.Unit),
                        ["Признак исключения"] = row.Exclude,
                        ["Изделие"] = Value(topProduct),
                        ["Наименование изделия"] = Value(topName),
                        ["Обозначение стандарта"] = Value(row.OboznStand)
                    };
                })

                // Порядок сортировки обеспечивает привычную структуру ведомости:
                // сначала обычные строки, затем исключённые разделы, внутри - вид изделия и наименование.
                .OrderBy(row => row["Признак исключения"])
                .ThenBy(row => row["Вид Изделия"] is string value ? value : string.Empty, StringComparer.Ordinal)
                .ThenBy(row => row["Наименование"] is string value ? value : string.Empty, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Получает сведения о версии объекта с использованием локального кэша.
        /// </summary>
        /// <param name="idVersion">Идентификатор версии объекта.</param>
        /// <returns>Сведения о версии объекта.</returns>
        private async Task<LoodsmanVersionInfo> GetVersion(int idVersion)
        {
            if (_versions.TryGetValue(idVersion, out var cached))
                return cached;

            var version = (await _apiClient.GetVersionInfo(idVersion)).FirstOrDefault()
                          ?? throw new InvalidOperationException($"Не удалось получить версию объекта {idVersion}");
            _versions[idVersion] = version;
            return version;
        }

        /// <summary>
        /// Получает атрибуты версии объекта с использованием локального кэша.
        /// </summary>
        /// <param name="idVersion">Идентификатор версии объекта.</param>
        /// <returns>Словарь значений атрибутов по их названиям.</returns>
        private async Task<Dictionary<string, string?>> GetAttributes(int idVersion)
        {
            if (_attributes.TryGetValue(idVersion, out var cached))
                return cached;

            var attrs = await _apiClient.GetAttributes([idVersion]);
            var result = attrs
                .Where(attr => !string.IsNullOrWhiteSpace(attr.Name))
                .GroupBy(attr => attr.Name!)
                .ToDictionary(group => group.Key, group => group.Last().Value);
            _attributes[idVersion] = result;
            return result;
        }

        /// <summary>
        /// Получает атрибуты связи с использованием локального кэша.
        /// </summary>
        /// <param name="idLink">Идентификатор связи.</param>
        /// <returns>Словарь значений атрибутов связи по их названиям.</returns>
        private async Task<Dictionary<string, string?>> GetLinkAttributes(int idLink)
        {
            if (_linkAttributes.TryGetValue(idLink, out var cached))
                return cached;

            var attrs = await _apiClient.GetLinkAttributes(idLink);
            var result = attrs
                .Where(attr => !string.IsNullOrWhiteSpace(attr.Name))
                .GroupBy(attr => attr.Name!)
                .ToDictionary(group => group.Key, group => group.Last().Value);
            _linkAttributes[idLink] = result;
            return result;
        }

        /// <summary>
        /// Получает связанные объекты по типу связи с использованием локального кэша.
        /// </summary>
        /// <param name="idVersion">Идентификатор версии исходного объекта.</param>
        /// <param name="linkType">Название типа связи.</param>
        /// <param name="inverse">Признак поиска по обратному направлению связи.</param>
        /// <returns>Список связанных объектов.</returns>
        private async Task<List<LoodsmanLinkedObject>> GetLinked(int idVersion, string linkType, bool inverse = false)
        {
            var key = (idVersion, linkType, inverse);
            if (_links.TryGetValue(key, out var cached))
                return cached;

            var links = await _apiClient.GetLinkedObjects(idVersion, linkType, inverse);
            _links[key] = links;
            return links;
        }

        /// <summary>
        /// Ищет документ заданного типа среди документов объекта.
        /// </summary>
        /// <param name="idVersion">Идентификатор версии объекта.</param>
        /// <param name="type">Название типа документа.</param>
        /// <returns>Найденный документ или null.</returns>
        private async Task<LoodsmanLinkedObject?> GetDocumentOfType(int idVersion, string type)
            => (await GetLinked(idVersion, LinkDocuments)).FirstOrDefault(doc => doc.Type == type);

        /// <summary>
        /// Определяет, является ли объект покупным.
        /// </summary>
        /// <param name="type">Тип объекта.</param>
        /// <param name="attrs">Атрибуты объекта.</param>
        /// <returns>True, если объект покупной.</returns>
        private static bool IsPurchased(string? type, Dictionary<string, string?> attrs)
            => type != TypeRepresentation && GetAttr(attrs, "Источник поступления") == "Покупное";

        /// <summary>
        /// Определяет, нужно ли раскрывать объект как узел состава.
        /// </summary>
        /// <param name="type">Тип объекта.</param>
        /// <returns>True, если объект может иметь значимый состав для отчёта.</returns>
        private static bool CanExpand(string? type)
            => type is TypeComplex or TypeComplect or TypeAssembly or TypeSpecifiedProduct
                or TypeAdditionalSection or TypeSelectionElement or TypeRepresentation;

        /// <summary>
        /// Определяет типы, которые не становятся самостоятельным родителем для покупных позиций.
        /// </summary>
        /// <param name="type">Тип объекта.</param>
        /// <returns>True для подборных элементов и представлений.</returns>
        private static bool IsTransparentType(string? type)
            => type is TypeSelectionElement or TypeRepresentation;

        /// <summary>
        /// Определяет, относится ли тип к специфицированным изделиям.
        /// </summary>
        /// <param name="type">Тип объекта.</param>
        /// <returns>True для комплексных, комплектных и сборочных типов.</returns>
        private static bool IsSpecifiedType(string? type)
            => type is TypeComplex or TypeComplect or TypeAssembly or TypeSpecifiedProduct;

        /// <summary>
        /// Определяет типы покупных изделий из справочников и стандартов.
        /// </summary>
        /// <param name="type">Тип объекта.</param>
        /// <returns>True для справочных, прочих и стандартных изделий.</returns>
        private static bool IsCatalogPurchaseType(string? type)
            => type is TypeOtherProduct or TypeOtherFromReference or TypeStandardProduct;

        /// <summary>
        /// Определяет типы объектов, которые могут быть покупной заготовкой.
        /// </summary>
        /// <param name="type">Тип объекта.</param>
        /// <returns>True, если тип может использоваться как материал или заготовка.</returns>
        private static bool IsPurchaseMaterialType(string? type)
            => type is TypeDetail or TypeOtherProduct or TypeOtherFromReference or TypeStandardProduct;

        /// <summary>
        /// Проверяет, находится ли строка состава ниже указанного предка.
        /// </summary>
        /// <param name="rows">Все строки состава.</param>
        /// <param name="ancestor">Предполагаемый предок.</param>
        /// <param name="candidate">Проверяемая строка.</param>
        /// <returns>True, если проверяемая строка является потомком.</returns>
        private static bool IsDescendant(List<CompositionRow> rows, CompositionRow ancestor, CompositionRow candidate)
        {
            var parentId = candidate.IdParent;
            while (parentId.HasValue)
            {
                if (parentId.Value == ancestor.IdChild)
                    return true;

                parentId = rows.LastOrDefault(row => row.IdChild == parentId.Value)?.IdParent;
            }

            return false;
        }

        /// <summary>
        /// Возвращает значение атрибута по названию.
        /// </summary>
        /// <param name="attrs">Словарь атрибутов.</param>
        /// <param name="name">Название атрибута.</param>
        /// <returns>Значение атрибута или null.</returns>
        private static string? GetAttr(Dictionary<string, string?> attrs, string name)
            => attrs.TryGetValue(name, out var value) ? value : null;

        /// <summary>
        /// Преобразует строковое значение в число с учётом русской и инвариантной культур.
        /// </summary>
        /// <param name="value">Строковое значение.</param>
        /// <param name="result">Результат преобразования.</param>
        /// <returns>True, если значение удалось преобразовать.</returns>
        private static bool TryParseDouble(string? value, out double result)
            => double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
               || double.TryParse(value, NumberStyles.Any, CultureInfo.GetCultureInfo("ru-RU"), out result);

        /// <summary>
        /// Преобразует пустую строку в объект пустого значения для совместимости с шаблоном отчёта.
        /// </summary>
        /// <param name="value">Исходное строковое значение.</param>
        /// <returns>Строка или пустой объект.</returns>
        private static object Value(string? value)
            => string.IsNullOrWhiteSpace(value) ? Empty() : value;

        /// <summary>
        /// Создаёт пустое значение отчёта в виде объекта.
        /// </summary>
        /// <returns>Пустой объект для сериализации как {}.</returns>
        private static object Empty()
            => new Dictionary<string, object?>();

        /// <summary>
        /// Приводит число к целому типу, если дробная часть отсутствует.
        /// </summary>
        /// <param name="value">Исходное числовое значение.</param>
        /// <returns>Целое или дробное числовое значение.</returns>
        private static object Number(double value)
            => Math.Abs(value % 1) < 0.0000001 ? Convert.ToInt32(value) : value;

        /// <summary>
        /// Внутренняя строка раскрытого состава изделия.
        /// </summary>
        private sealed class CompositionRow
        {
            /// <summary>
            /// Порядковый номер строки состава.
            /// </summary>
            public int Nn { get; set; }

            /// <summary>
            /// Идентификатор непосредственного родителя.
            /// </summary>
            public int? IdParent { get; set; }

            /// <summary>
            /// Идентификатор смыслового родителя с учётом подборных элементов и представлений.
            /// </summary>
            public int? IdParent2 { get; set; }

            /// <summary>
            /// Идентификатор дочернего объекта.
            /// </summary>
            public int IdChild { get; set; }

            /// <summary>
            /// Идентификатор дочернего объекта после пропуска прозрачных типов.
            /// </summary>
            public int? IdChild2 { get; set; }

            /// <summary>
            /// Идентификатор связи с родителем.
            /// </summary>
            public int? IdLink { get; set; }

            /// <summary>
            /// Корневой объект разбираемой ветки состава.
            /// </summary>
            public int IdRoot { get; set; }

            /// <summary>
            /// Название типа объекта.
            /// </summary>
            public string? Type { get; set; }

            /// <summary>
            /// Обозначение объекта.
            /// </summary>
            public string? Product { get; set; }

            /// <summary>
            /// Количество в непосредственной связи.
            /// </summary>
            public double Qty { get; set; }

            /// <summary>
            /// Накопленное количество от корня.
            /// </summary>
            public double QtyAll { get; set; }

            /// <summary>
            /// Количество на регулировку.
            /// </summary>
            public double? QtyRegulirovka { get; set; }

            /// <summary>
            /// Единица измерения количества.
            /// </summary>
            public string? Unit { get; set; }

            /// <summary>
            /// Признак покупного объекта.
            /// </summary>
            public bool IsPokup { get; set; }

            /// <summary>
            /// Признак принадлежности к комплекту.
            /// </summary>
            public bool InComplect { get; set; }

            /// <summary>
            /// Принадлежность по спецификации комплекта.
            /// </summary>
            public string? Accessory { get; set; }
        }

        /// <summary>
        /// Правило удаления строки состава или замены детали на заготовку.
        /// </summary>
        /// <param name="Id">Идентификатор объекта, к которому применяется правило.</param>
        /// <param name="DelSelf">Признак удаления самой строки.</param>
        /// <param name="IdZagotovka">Идентификатор покупной заготовки, если деталь нужно заменить.</param>
        private sealed record DeleteEntry(int Id, bool DelSelf, int? IdZagotovka);

        /// <summary>
        /// Внутренняя строка отчёта перед финальной группировкой и форматированием колонок.
        /// </summary>
        private sealed class IntermediateReportRow
        {
            /// <summary>
            /// Идентификатор ДСЕ или заменяющей заготовки.
            /// </summary>
            public int IdDse { get; set; }

            /// <summary>
            /// Идентификатор исходного объекта из состава.
            /// </summary>
            public int IdChild { get; set; }

            /// <summary>
            /// Тип исходного объекта.
            /// </summary>
            public string? Type { get; set; }

            /// <summary>
            /// Наименование для отчёта.
            /// </summary>
            public string? Name { get; set; }

            /// <summary>
            /// Обозначение объекта.
            /// </summary>
            public string? Product { get; set; }

            /// <summary>
            /// Вид изделия.
            /// </summary>
            public string? Vid { get; set; }

            /// <summary>
            /// Обозначение документа на поставку.
            /// </summary>
            public string? DocumentOboznachenie { get; set; }

            /// <summary>
            /// Типоразмер.
            /// </summary>
            public string? Tiporazmer { get; set; }

            /// <summary>
            /// Обозначение родителя или спецификации комплекта.
            /// </summary>
            public string? Parent { get; set; }

            /// <summary>
            /// Код продукции.
            /// </summary>
            public string? CodProdukcii { get; set; }

            /// <summary>
            /// Поставщик.
            /// </summary>
            public string? Postavshik { get; set; }

            /// <summary>
            /// Количество на изделие.
            /// </summary>
            public double QtyIzd { get; set; }

            /// <summary>
            /// Количество в комплекты.
            /// </summary>
            public double QtyKomplekty { get; set; }

            /// <summary>
            /// Количество на регулировку.
            /// </summary>
            public double QtyRegulirovka { get; set; }

            /// <summary>
            /// Общее количество до финального форматирования.
            /// </summary>
            public double QtyAll { get; set; }

            /// <summary>
            /// Единица измерения количества.
            /// </summary>
            public string? Unit { get; set; }

            /// <summary>
            /// Признак строки-исключения с собственной ведомостью покупных изделий.
            /// </summary>
            public int Exclude { get; set; }

            /// <summary>
            /// Обозначение стандарта.
            /// </summary>
            public string? OboznStand { get; set; }
        }
    }
}
