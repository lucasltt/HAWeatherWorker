using System;
using System.Collections.Generic;
using System.Text;

namespace HAWeatherWorker
{
    public class StormSummary
    {
        public double NearestStormDistance { get; set; }

        public int Storms1KM { get; set; }

        public int Storms2KM { get; set; }

        public int Storms4KM { get; set; }

        public int Storms8KM { get; set; }

        public string LastStormDate { get; set; } = "Nunca";

        public string StatusChuva { get; set; } = "Sem Chuva";



        private Queue<int> ValoresChuva = new Queue<int>();

        public void AddChuva(int valorAnalogico)
        {
            ValoresChuva.Enqueue(valorAnalogico);

            if (ValoresChuva.Count > 5) ValoresChuva.Dequeue();

            int[] arrayChuva = ValoresChuva.ToArray();
            int valorMedio = default(int);

            foreach (int chuva in arrayChuva) valorMedio += chuva;

            valorMedio = valorMedio / arrayChuva.Length;

            if (valorMedio >= 1000) StatusChuva = "Sem Chuva";
            if (valorMedio >= 700 && valorMedio < 1000) StatusChuva = "Chuva Fraca";
            if (valorMedio >= 500 && valorMedio < 700) StatusChuva = "Chuva Média";
            if (valorMedio < 500) StatusChuva = "Chuva Forte";
        }




    }
}
