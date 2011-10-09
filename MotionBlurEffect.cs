using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Effects;
using System.Windows;
using System.Windows.Media;
using System.Reflection;

public class MotionBlurEffect : ShaderEffect
{
	//Obtained from myBoard source code, not useful currently

	/*public static readonly DependencyProperty InputProperty = ShaderEffect.RegisterPixelShaderSamplerProperty("Input", typeof(MotionBlurEffect), 0);
	public static readonly DependencyProperty AngleProperty = DependencyProperty.Register("Angle", typeof(double), typeof(MotionBlurEffect), new UIPropertyMetadata(((double)(0)), PixelShaderConstantCallback(0)));
	public static readonly DependencyProperty BlurAmountProperty = DependencyProperty.Register("BlurAmount", typeof(double), typeof(MotionBlurEffect), new UIPropertyMetadata(((double)(0)), PixelShaderConstantCallback(1)));
	public MotionBlurEffect()
	{
		PixelShader pixelShader = new PixelShader();
		pixelShader.UriSource = MakePackUri("MotionBlur.ps");
		this.PixelShader = pixelShader;

		this.UpdateShaderValue(InputProperty);
		this.UpdateShaderValue(AngleProperty);
		this.UpdateShaderValue(BlurAmountProperty);
	}

	public static Uri MakePackUri(string relativeFile)
	{
		string uriString = "pack://application:,,,/" + AssemblyShortName + ";component/" + relativeFile;
		return new Uri(uriString);
	}

	private static string AssemblyShortName
	{
		get
		{
			if (_assemblyShortName == null)
			{
				Assembly a = typeof(MotionBlurEffect).Assembly;

				// Pull out the short name.
				_assemblyShortName = a.ToString().Split(',')[0];
			}

			return _assemblyShortName;
		}
	}
	private static string _assemblyShortName;

	public Brush Input
	{
		get
		{
			return ((Brush)(this.GetValue(InputProperty)));
		}
		set
		{
			this.SetValue(InputProperty, value);
		}
	}
	public double Angle
	{
		get
		{
			return ((double)(this.GetValue(AngleProperty)));
		}
		set
		{
			this.SetValue(AngleProperty, value);
		}
	}
	public double BlurAmount
	{
		get
		{
			return ((double)(this.GetValue(BlurAmountProperty)));
		}
		set
		{
			this.SetValue(BlurAmountProperty, value);
		}
	}*/
}