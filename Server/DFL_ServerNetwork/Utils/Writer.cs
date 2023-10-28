using System;

namespace DFL.Utils
{
    public static class Writer
    {
        public static void Write(string text, ConsoleColor color, bool inline = true)
        {
            Console.ForegroundColor = color;
            Console.Write(text + (inline ? "\n\r" : ""));
        }

        public static void Write(string text, bool inline = true)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(text + (inline ? "\n\r" : ""));
        }

        public static void Write_Database(string text, ConsoleColor color, bool inline = true)
        {
            Database();
            Console.ForegroundColor = color;
            Console.Write(text + (inline ? "\n\r" : ""));
        }

        public static void Write_Physical(string text, ConsoleColor color, bool inline = true)
        {
            Physical();
            Console.ForegroundColor = color;
            Console.Write(text + (inline ? "\n\r" : ""));
        }

        public static void Write_Spells(string text, ConsoleColor color, bool inline = true)
        {
            Spells();
            Console.ForegroundColor = color;
            Console.Write(text + (inline ? "\n\r" : ""));
        }

        public static void Write_Monsters(string text, ConsoleColor color, bool inline = true)
        {
            Monsters();
            Console.ForegroundColor = color;
            Console.Write(text + (inline ? "\n\r" : ""));
        }
        public static void Write_Fight(string text, ConsoleColor color, bool inline = true)
        {
            Fight();
            Console.ForegroundColor = color;
            Console.Write(text + (inline ? "\n\r" : ""));
        }
        public static void Write_Server(string text, ConsoleColor color, bool inline = true)
        {
            Server();
            Console.ForegroundColor = color;
            Console.Write(text + (inline ? "\n\r" : ""));
        }
        public static void Write_PNJ(string text, ConsoleColor color, bool inline = true)
        {
            PNJ();
            Console.ForegroundColor = color;
            Console.Write(text + (inline ? "\n\r" : ""));
        }

        public static void Database()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[Database] ");
        }
        public static void Physical()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[Physical Persistance] ");
        }
        public static void Spells()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[Spells] ");
        }
        public static void Monsters()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[Monsters] ");
        }
        public static void Fight()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[Round Fight] ");
        }
        public static void Server()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[Server Settings] ");
        }
        public static void PNJ()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("[PNJ] ");
        }
    }
}