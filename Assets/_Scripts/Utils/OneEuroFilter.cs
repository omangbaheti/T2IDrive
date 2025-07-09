using UnityEngine;
using System;

public class OneEuroFilter
{
    private float freq;
    private float minCutoff;
    private float beta;
    private float dCutoff;

    private float xPrev;
    private float dxPrev;
    private bool initialized;

    public OneEuroFilter(float freq = 60.0f, float minCutoff = 1.0f, float beta = 0.0f, float dCutoff = 1.0f)
    {
        this.freq = freq;
        this.minCutoff = minCutoff;
        this.beta = beta;
        this.dCutoff = dCutoff;
        this.initialized = false;
    }

    private float Alpha(float cutoff)
    {
        float tau = 1.0f / (2.0f * Mathf.PI * cutoff);
        float te = 1.0f / freq;
        return 1.0f / (1.0f + tau / te);
    }

    private float ExponentialSmoothing(float a, float x, float xPrev)
    {
        return a * x + (1.0f - a) * xPrev;
    }

    public float Filter(float x)
    {
        if (!initialized)
        {
            xPrev = x;
            dxPrev = 0.0f;
            initialized = true;
        }

        // Estimate derivative
        float dx = (x - xPrev) * freq;
        float alphaD = Alpha(dCutoff);
        dxPrev = ExponentialSmoothing(alphaD, dx, dxPrev);

        // Compute cutoff
        float cutoff = minCutoff + beta * Mathf.Abs(dxPrev);
        float alpha = Alpha(cutoff);

        // Filter signal
        xPrev = ExponentialSmoothing(alpha, x, xPrev);
        return xPrev;
    }

    public void SetFrequency(float newFreq)
    {
        freq = newFreq;
    }

    public void SetParameters(float newMinCutoff, float newBeta)
    {
        minCutoff = newMinCutoff;
        beta = newBeta;
    }

    public void Reset()
    {
        initialized = false;
    }
}