using Ascon.Plm.DataPacket;
using Ascon.Plm.Mapping;

namespace CSharpServerAPI
{
    /// <summary>
    /// Предоставляет методы для преобразования данных из формата DataPacket (массив байт) 
    /// в объекты доменной модели с использованием маппера <see cref="DbMapper"/>.
    /// </summary>
    /// <remarks>
    /// Класс является обёрткой над <see cref="DbMapper"/> и <see cref="DataPacketReader"/>, обеспечивающей удобное 
    /// преобразование бинарных данных, полученных от Server API ЛОЦМАН:PLM, в строго типизированные объекты.
    /// </remarks>
    public static class Mapper
    {
        /// <summary>
        /// Создаёт провайдер для чтения данных из базы данных ЛОЦМАН:PLM, полученных из Server API.
        /// </summary>
        /// <param name="data">Массив байт в формате DataPacket, полученный от Server API ЛОЦМАН:PLM.</param>
        /// <exception cref="ArgumentException"/>
        private static DataPacketReader CreateReader(object data)
        {
            if (data is byte[] bytes)
            {
                return new DataPacketReader(bytes);
            }
            else
            {
                throw new ArgumentException($"Аргумент {nameof(data)} должен быть массивом байт", nameof(data));
            }
        }

        /// <summary>
        /// Создаёт <see cref="DataPacketReader"/> из переданных данных и возвращает первый элемент последовательности или null, 
        /// если последовательность пуста.
        /// </summary>
        /// <param name="data">Массив байт в формате DataPacket, полученный от Server API ЛОЦМАН:PLM.</param>
        public static T FirstOrDefault<T>(object data) where T : class
        {
            using var reader = CreateReader(data);

            return DbMapper.FirstOrDefault<T>(reader);
        }

        /// <summary>
        /// Создаёт <see cref="DataPacketReader"/> из переданных данных и преобразует все записи в список объектов указанного типа.
        /// </summary>
        /// <param name="data">Массив байт в формате DataPacket, полученный от Server API ЛОЦМАН:PLM.</param>
        public static List<T> ToList<T>(object data) where T : class
        {
            using var reader = CreateReader(data);

            return DbMapper.ToList<T>(reader);
        }
    }
}
