using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;


float dt = 0.025f;
float K = 10;

float Kp = 0.75f;

float Ki = 2f;
float intEr = 0f;

float Kd = -0.05f;
float lastEr = 0f;

float pid(float erro)
{
    intEr += erro * dt / K;
    float result = Kp * erro + Ki * intEr + Kd * (erro - lastEr);
    lastEr = erro;
    return result;
}

float system(float input, float output)
{
    return pid(input - output);
}

float inputNeeded(float t)
{
    return 15f;
}

float t = 0;
float input = 15f;
float output = 0f;
float outputPDI = 0f;
Queue<float> ins = new Queue<float>();
Queue<float> outs = new Queue<float>();
Queue<float> outspdi = new Queue<float>();

Queue<float> mainerror = new Queue<float>();
Queue<float> pdierror = new Queue<float>();

float controller(float force)
{
    return force / 3;
}

float realModel(float force)
{
    return 2998 * 
        (float)Math.Sin(force / 1000f);
}

// Noises
int bigtouch = 0;
var seed = DateTime.Now.Millisecond;
Random rand = new Random(seed);
float constantNoise = -.75f;
float noiseScale = 4f;
Queue<float> noise = new Queue<float>();
float totalNoise = 0f;

ApplicationConfiguration.Initialize();

var form = new Form();
form.WindowState = FormWindowState.Maximized;
form.FormBorderStyle = FormBorderStyle.None;

PictureBox pb = new PictureBox();
pb.Dock = DockStyle.Fill;
form.Controls.Add(pb);

Bitmap bmp = null;
Graphics g = null;

Timer tm = new Timer();
tm.Interval = 25;

form.KeyDown += (o, e) =>
{
    if (e.KeyCode == Keys.Escape)
        Application.Exit();
    
    if (e.KeyCode == Keys.Space)
        bigtouch = 200;
};

form.Load += delegate
{
    bmp = new Bitmap(pb.Width, pb.Height);
    g = Graphics.FromImage(bmp);
    g.Clear(Color.White);
    pb.Image = bmp;
    tm.Start();
};
tm.Tick += delegate
{
    tick();
    
    if (outs.Count < 2)
        return;
    
    g.Clear(Color.White);
    g.DrawRectangle(Pens.Black, 
        50, 50, 1000, 1000);
    for (int i = 0; i < 21; i++)
        g.DrawString(i.ToString(),
            SystemFonts.CaptionFont,
            Brushes.Black,
            new PointF(1050, 1050 - 50 * i));
    
    Pen pen = new Pen(Color.SkyBlue, 3f);
    g.DrawLines(pen, 
        outs.Select((o, i) => new PointF(
            50f + 10f * i, 50f + 1000f - o * 50f
        )).ToArray());
    
    Pen pen2 = new Pen(Color.DarkGreen, 3f);
    g.DrawLines(pen2, 
        outspdi.Select((o, i) => new PointF(
            50f + 10f * i, 50f + 1000f - o * 50f
        )).ToArray());
    
    Pen pen3 = new Pen(Color.Red, 3f);
    g.DrawLines(pen3, 
        ins.Select((o, i) => new PointF(
            50f + 10f * i, 50f + 1000f - o * 50f
        )).ToArray());
    
    g.DrawString($"Erro Médio: {mainerror.Average()}",
        SystemFonts.CaptionFont,
        Brushes.SkyBlue, new PointF(1070, 50));
    g.DrawString($"Erro Médio: {pdierror.Average()}",
        SystemFonts.CaptionFont,
        Brushes.DarkGreen, new PointF(1070, 100));
    
    pb.Refresh();
};

void tick()
{
    for (int i = 0; i < K; i++)
    {
        t += dt / K;
        input = inputNeeded(t);

        if (noise.Count >= K)
            totalNoise -= noise.Dequeue();
        var newNoise = 2f * noiseScale * (rand.NextSingle() - .5f);
        if (bigtouch > 0)
        {
            bigtouch--;
            newNoise = newNoise > 0f ?
                -newNoise : newNoise;
            newNoise *= 2;
        }
        noise.Enqueue(newNoise);
        totalNoise += newNoise;

        float realInput = controller(input);
        output = realModel(realInput) 
            + totalNoise / 10
            + constantNoise;

        float realInputPDI = controller(system(input, outputPDI));
        outputPDI = realModel(realInputPDI) 
            + totalNoise / 10
            + constantNoise;
        
        var mainer = output < input ? input - output : output - input;
        mainerror.Enqueue(mainer);
        if (mainerror.Count > K * 40)
            mainerror.Dequeue();
        
        var pdier = outputPDI < input ? input - outputPDI : outputPDI - input;
        pdierror.Enqueue(pdier);
        if (pdierror.Count > K * 40)
            pdierror.Dequeue();
    }
    if (ins.Count > 100)
        ins.Dequeue();
    ins.Enqueue(input);

    if (outs.Count > 100)
        outs.Dequeue();
    outs.Enqueue(output);

    if (outspdi.Count > 100)
        outspdi.Dequeue();
    outspdi.Enqueue(outputPDI);
}

Application.Run(form);