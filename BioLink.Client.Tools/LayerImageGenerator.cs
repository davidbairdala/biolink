﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BioLink.Client.Tools {

    public static class LayerImageGenerator {

        public static BitmapSource GetImageForLayer(GridLayer layer, Color lowcolor, Color highcolor, Color novaluecolor, double cutoff = 0, int intervals = 256) {
            var palette = CreateGradientPalette(lowcolor, highcolor, novaluecolor, intervals);
            var bmp = new WriteableBitmap(layer.Width, layer.Height, 96, 96, PixelFormats.Indexed8, palette);

            var range = layer.GetRange();

            if (cutoff == 0) {
                cutoff = range.Min;
            }

            double dx = Math.Abs(range.Max - cutoff) / (intervals - 1);
            byte[] array = new byte[layer.Width * layer.Height];
            byte index = 0;
            for (int y = 0; y < layer.Height; y++) {
                for (int x = 0; x < layer.Width; x++) {
                    var value = layer.GetCellValue(x, y);
                    if (value == layer.NoValueMarker) {
                        index = 0;
                    } else {
                        if (value >= cutoff || cutoff == 0) {
                            index = (byte) (((value - cutoff) / dx) + 1);
                        } else {
                            index = 0;
                        }
                    }
                    array[(((layer.Height - 1) - y) * layer.Width) + x] = index;
                }
            }

            Int32Rect r= new Int32Rect(0, 0, layer.Width, layer.Height);

            bmp.WritePixels(r, array, layer.Width, 0, 0);

            return bmp;
        }

        public static BitmapPalette CreateGradientPalette(Color lowcolor, Color highcolor, Color noValueColor, int intervals = 256) {
            var palette = new Color[intervals];
            var r1 = lowcolor.R;
            var g1 = lowcolor.G;
            var b1 = lowcolor.B;
            var r2 = highcolor.R;
            var g2 = highcolor.G;
            var b2 = highcolor.B;

            float deltaR = ((float)(r2 - r1)) / ((float)(intervals - 1));
            float deltaG = ((float)(g2 - g1)) / ((float)(intervals - 1));
            float deltaB = ((float)(b2 - b1)) / ((float)(intervals - 1));

            float r = r1;
            float g = g1;
            float b = b1;


            for (int i = 1; i <= intervals && i <= 255; ++i) {
                palette[i] = Color.FromRgb((byte) r, (byte) g, (byte) b);
                r += deltaR;
                g += deltaG;
                b += deltaB;
            }

            palette[0] = noValueColor;

            return new BitmapPalette(palette);
        }

    }
}