using System;

namespace Server.Engines.Craft
{
    public class CraftSubRes
    {
        public CraftSubRes(Type type, TextDefinition name, double reqSkill, TextDefinition message) : this(
            type,
            name,
            reqSkill,
            0,
            message
        )
        {
        }

        public CraftSubRes(
            Type type, TextDefinition name, double reqSkill, int genericNameNumber, TextDefinition message,
            Systems.MahaonMetals.MahaonMetal? metal = null
        )
        {
            ItemType = type;
            Name = name;
            RequiredSkill = reqSkill;
            GenericNameNumber = genericNameNumber;
            Message = message;
            Metal = metal;
        }

        public Type ItemType { get; }

        /// <summary>
        ///     Какой из наших металлов стоит за этой строкой, если строка металлическая.
        ///
        ///     Понадобилось потому, что у всех 24 металлов ОДИН тип C# (MahaonIngot,
        ///     металл лежит полем), а движок различает подресурсы только по типу. Тип здесь
        ///     у всех одинаковый и различать им нечего — различает это поле, а выбранную
        ///     строку движок и так помнит номером в CraftContext.LastResourceIndex.
        ///
        ///     null у всего ванильного: дерево, кожа, чешуя, ткань. Там тип по-прежнему
        ///     единственный признак, и ничего не меняется.
        /// </summary>
        public Systems.MahaonMetals.MahaonMetal? Metal { get; }

        public TextDefinition Name { get; }

        public int GenericNameNumber { get; }

        public TextDefinition Message { get; }

        public double RequiredSkill { get; }
    }
}
