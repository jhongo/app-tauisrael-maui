using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppMobileTaS.Views;

public partial class vHome : ContentPage
{
    private const double PesoSeguimiento = 0.3;
    private const double PesoExamen = 0.2;
    private const double NotaMinima = 0;
    private const double NotaMaxima = 10;

    public vHome()
    {
        InitializeComponent();
        dpFecha.Date = DateTime.Today;
    }

    // Recalcula los parciales en pantalla mientras el usuario escribe
    private void OnNotaChanged(object? sender, TextChangedEventArgs e)
    {
        double parcial1 = CalcularParcialEnVivo(txtSeguimiento1.Text, txtExamen1.Text);
        double parcial2 = CalcularParcialEnVivo(txtSeguimiento2.Text, txtExamen2.Text);

        lblParcial1.Text = parcial1.ToString("F2");
        lblParcial2.Text = parcial2.ToString("F2");
    }

    private async void OnCalcularClicked(object? sender, EventArgs e)
    {
        // Validar estudiante
        if (pkEstudiante.SelectedIndex == -1)
        {
            await DisplayAlertAsync("Error", "Seleccione un estudiante.", "OK");
            return;
        }

        // Validar notas
        if (!await ValidarNota(txtSeguimiento1, "Nota Seguimiento 1", out double seguimiento1)) return;
        if (!await ValidarNota(txtExamen1, "Examen 1", out double examen1)) return;
        if (!await ValidarNota(txtSeguimiento2, "Nota Seguimiento 2", out double seguimiento2)) return;
        if (!await ValidarNota(txtExamen2, "Examen 2", out double examen2)) return;

        // Cálculos
        double parcial1 = seguimiento1 * PesoSeguimiento + examen1 * PesoExamen;
        double parcial2 = seguimiento2 * PesoSeguimiento + examen2 * PesoExamen;
        double notaFinal = parcial1 + parcial2;
        string estado = ObtenerEstado(notaFinal);

        lblParcial1.Text = parcial1.ToString("F2");
        lblParcial2.Text = parcial2.ToString("F2");
        lblNotaFinal.Text = notaFinal.ToString("F2");
        MostrarEstado(estado, notaFinal);

        string mensaje =
            "Nombre: " + pkEstudiante.SelectedItem + "\n" +
            "Fecha: " + dpFecha.Date?.ToString("dd/MM/yyyy") + "\n" +
            "Nota Parcial 1: " + parcial1.ToString("F2") + "\n" +
            "Nota Parcial 2: " + parcial2.ToString("F2") + "\n" +
            "Nota Final: " + notaFinal.ToString("F2") + "\n" +
            "Estado: " + estado;

        await DisplayAlertAsync("Resultado", mensaje, "OK");
    }

    private void OnLimpiarClicked(object? sender, EventArgs e)
    {
        pkEstudiante.SelectedIndex = -1;
        dpFecha.Date = DateTime.Today;
        txtSeguimiento1.Text = string.Empty;
        txtExamen1.Text = string.Empty;
        txtSeguimiento2.Text = string.Empty;
        txtExamen2.Text = string.Empty;
        lblNotaFinal.Text = "0.00";

        bool oscuro = Application.Current?.RequestedTheme == AppTheme.Dark;
        lblEstado.Text = "Sin calcular";
        lblEstado.TextColor = Color.FromArgb(oscuro ? "#94A3B8" : "#64748B");
        brdEstado.BackgroundColor = Color.FromArgb(oscuro ? "#334155" : "#E2E8F0");
        pbNotaFinal.ProgressColor = Color.FromArgb("#3B82F6");
        pbNotaFinal.ProgressTo(0, 250, Easing.CubicOut);
    }

    // Pinta el badge de estado y la barra de progreso con el color semántico
    private void MostrarEstado(string estado, double notaFinal)
    {
        (string texto, string fondo) = estado switch
        {
            "APROBADO" => ("#16A34A", "#DCFCE7"),
            "COMPLEMENTARIO" => ("#D97706", "#FEF3C7"),
            _ => ("#DC2626", "#FEE2E2")
        };

        lblEstado.Text = estado;
        lblEstado.TextColor = Color.FromArgb(texto);
        brdEstado.BackgroundColor = Color.FromArgb(fondo);
        pbNotaFinal.ProgressColor = Color.FromArgb(texto);
        pbNotaFinal.ProgressTo(notaFinal / NotaMaxima, 400, Easing.CubicOut);
    }

    // Valida que el campo no esté vacío, sea numérico y esté entre 0 y 10
    private Task<bool> ValidarNota(Entry entry, string nombreCampo, out double nota)
    {
        string texto = entry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(texto))
        {
            nota = 0;
            return MostrarError($"Ingrese la {nombreCampo}.", entry);
        }

        if (!TryParseNota(texto, out nota))
        {
            return MostrarError($"La {nombreCampo} debe ser un valor numérico.", entry);
        }

        if (nota < NotaMinima || nota > NotaMaxima)
        {
            return MostrarError($"La {nombreCampo} debe estar entre {NotaMinima} y {NotaMaxima}.", entry);
        }

        return Task.FromResult(true);
    }

    private async Task<bool> MostrarError(string mensaje, Entry entry)
    {
        await DisplayAlertAsync("Error", mensaje, "OK");
        entry.Focus();
        return false;
    }

    // Acepta coma o punto como separador decimal
    private static bool TryParseNota(string? texto, out double nota)
    {
        string normalizado = (texto ?? string.Empty).Trim().Replace(',', '.');
        return double.TryParse(normalizado, NumberStyles.Float, CultureInfo.InvariantCulture, out nota)
               && !double.IsNaN(nota) && !double.IsInfinity(nota);
    }

    // Devuelve el parcial si ambas notas son válidas; si no, 0
    private static double CalcularParcialEnVivo(string? seguimiento, string? examen)
    {
        if (!TryParseNota(seguimiento, out double s) || s < NotaMinima || s > NotaMaxima) s = 0;
        if (!TryParseNota(examen, out double x) || x < NotaMinima || x > NotaMaxima) x = 0;
        return s * PesoSeguimiento + x * PesoExamen;
    }

    private static string ObtenerEstado(double notaFinal)
    {
        if (notaFinal >= 7) return "APROBADO";
        if (notaFinal >= 5) return "COMPLEMENTARIO";
        return "REPROBADO";
    }
}
