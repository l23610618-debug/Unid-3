// Mini SBR - Control de acceso y seguridad (consola C#, autonomo)
// Base de Hechos, Base de Reglas y Motor de Inferencia por separado.
// Salida: solo las reglas disparadas (id | nombre | prioridad, 1 = mas alta).

using System;
using System.Collections.Generic;
using System.Linq;

// ---------- BASE DE REGLAS ----------
class Regla
{
    public string Id, Nombre, Conclusion;
    public int Prioridad;
    public string[] Premisas;

    public Regla(string id, string nombre, int prioridad, string[] premisas, string conclusion)
    {
        Id = id; Nombre = nombre; Prioridad = prioridad; Premisas = premisas; Conclusion = conclusion;
    }

    public override string ToString() =>
        Id + " | " + Nombre + " | Prioridad " + Prioridad;
}

class BaseDeReglas
{
    public List<Regla> Reglas = new List<Regla>
    {
        // Diagnostico
        new Regla("R1", "Credencial vencida",  3, new[] { "credencial_estado=vencida", "usuario_registrado=si" }, "diagnostico=Credencial vencida"),
        new Regla("R2", "Intentos fallidos",   1, new[] { "intentos_fallidos>=3" }, "diagnostico=Posible intento de acceso no autorizado"),
        new Regla("R3", "Acceso sospechoso",   2, new[] { "fuera_de_turno=si", "ubicacion_conocida=no" }, "diagnostico=Acceso sospechoso"),
        // Accion
        new Regla("R4", "Solicitar MFA",       2, new[] { "diagnostico=Acceso sospechoso" }, "accion=Solicitar verificacion MFA"),
        new Regla("R5", "Bloquear cuenta",     1, new[] { "diagnostico=Posible intento de acceso no autorizado" }, "accion=Bloquear cuenta y avisar al administrador")
    };
}

// ---------- MOTOR DE INFERENCIA ----------
class Motor
{
    HashSet<string> hechos;   // BASE DE HECHOS
    BaseDeReglas reglas;
    public int Disparadas = 0;

    public Motor(HashSet<string> hechos, BaseDeReglas reglas)
    {
        this.hechos = hechos; this.reglas = reglas;
    }

    // Hacia ADELANTE: en cada paso dispara la regla aplicable de mayor prioridad
    public void Adelante()
    {
        while (true)
        {
            var r = reglas.Reglas
                .Where(x => x.Premisas.All(hechos.Contains) && !hechos.Contains(x.Conclusion))
                .OrderBy(x => x.Prioridad)
                .FirstOrDefault();

            if (r == null) break;

            hechos.Add(r.Conclusion);
            Disparadas++;
            Console.WriteLine(r);
        }
    }

    // Hacia ATRAS: demuestra una meta buscando las reglas que la concluyen
    public bool Atras(string meta)
    {
        if (hechos.Contains(meta)) return true;

        foreach (var r in reglas.Reglas.Where(x => x.Conclusion == meta).OrderBy(x => x.Prioridad))
        {
            if (r.Premisas.All(Atras))
            {
                hechos.Add(meta);
                Disparadas++;
                Console.WriteLine(r);
                return true;
            }
        }
        return false;
    }
}

// ---------- PROGRAMA PRINCIPAL ----------
class Program
{
    static void Main()
    {
        var reglas = new BaseDeReglas();

        // Hacia adelante: todos los datos presentes
        var hechos1 = new HashSet<string> { "credencial_estado=vencida", "usuario_registrado=si",
            "intentos_fallidos>=3", "fuera_de_turno=si", "ubicacion_conocida=no" };
        new Motor(hechos1, reglas).Adelante();

        Console.WriteLine();

        // Hacia atras: meta "bloquear cuenta" con intentos fallidos
        var hechos2 = new HashSet<string> { "usuario_registrado=si", "intentos_fallidos>=3" };
        new Motor(hechos2, reglas).Atras("accion=Bloquear cuenta y avisar al administrador");
    }
}