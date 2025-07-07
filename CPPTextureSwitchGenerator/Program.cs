using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace CPPTextureSwitchGenerator
{
	class Program
	{
		static void WriteConstructor(StreamWriter tw, BinaryWriter bw, String name, String source)
		{
			const bool loadData = true;
			const String unknown = "ID_UNKNOWN";
			tw.WriteLine("public:");

			Dictionary<String, String> filePathMap = new Dictionary<string,string>();

			String[] files = Directory.GetFiles(source);
			foreach (String file in files)
			{
				String resName = Path.GetFileNameWithoutExtension(file);
				if (resName == "_")
				{
					resName = unknown;
				}
				String ext = Path.GetExtension(file);
				if (ext.ToLowerInvariant()==".png")
				{
					filePathMap[resName] = file;
				}
			}

			tw.WriteLine("\tenum ResourceID {");
			foreach (String resName in filePathMap.Keys)
			{
				tw.WriteLine("\t\t" + resName + ",");
			}
			tw.WriteLine("\t};");

			tw.WriteLine("\t" + name + "()");
			tw.WriteLine("\t{");
			tw.WriteLine("\t\tm_texture = NULL;");
			tw.WriteLine("\t};");

			tw.WriteLine("\t\tResourceID IDFromName(const char* name)");
			tw.WriteLine("\t{");
			foreach (String resName in filePathMap.Keys)
			{
				tw.Write("\t\tif (strcmp(name,\"");
				tw.Write(resName);
				tw.WriteLine("\")==0)");
				tw.WriteLine("\t\t{");
				tw.Write("\t\t\treturn ");
				tw.Write(resName);
				tw.WriteLine(";");
				tw.WriteLine("\t\t}");
			}
			tw.Write("\t\treturn ");
			tw.Write(unknown);
			tw.WriteLine(";");
			tw.WriteLine("\t}");

			tw.WriteLine("\tvoid Init(ResourceID id, ID3D11Device* device)");
			tw.WriteLine("\t{");
			tw.WriteLine("\t\tconst int dimension = 1024;");
			tw.WriteLine("\t\tconst int buffersize = 4 * dimension * dimension;");
			if (loadData)
			{
				tw.WriteLine("\t\tHRSRC resourceHandle = ::FindResource(NULL, MAKEINTRESOURCE(IDR_RCDATA1), RT_RCDATA);");
				tw.WriteLine("\t\tHGLOBAL resourceData = ::LoadResource(NULL, resourceHandle);");
				tw.WriteLine("\t\tchar* rawData = (char*)::LockResource(resourceData);");
			}
			else
			{
				tw.WriteLine("\t\tchar* rawData = new char[buffersize];");
				tw.WriteLine("\t\tfor (int p=0; p<buffersize; ++p) { rawData[p] = ((p % 4) == 0) ? 255 : 0; }");
			}
			tw.WriteLine("\t\tD3D11_SUBRESOURCE_DATA imageData;");

			tw.WriteLine("\t\tswitch (id)");
			tw.WriteLine("\t\t{");

			foreach (String id in filePathMap.Keys)
			{
				tw.WriteLine("\t\t\tcase " + id + ":");
				if (id==unknown)
				{
					tw.WriteLine("\t\t\tdefault:");
				}
				tw.WriteLine("\t\t\t\t{");
				WriteBitmap(tw, bw, "\t\t\t\t\t", filePathMap[id]);
				tw.WriteLine("\t\t\t\t\tbreak;");
				tw.WriteLine("\t\t\t}");
			}
			if (!loadData)
			{
				tw.WriteLine("\t\tdelete[] rawData;");
			}
			tw.WriteLine("\t\t}");
			tw.WriteLine("\t};");
		}

		static void WriteBitmap(StreamWriter tw, BinaryWriter bw, String prefix, String bitmapPath)
		{
			Bitmap sourceData = new Bitmap(bitmapPath);
			tw.WriteLine(prefix + "D3D11_TEXTURE2D_DESC desc;");
			tw.WriteLine(prefix + "desc.Width = " + sourceData.Width.ToString() + ";");
			tw.WriteLine(prefix + "desc.Height = " + sourceData.Height.ToString() + ";");
			tw.WriteLine(prefix + "desc.MipLevels = 1;");
			tw.WriteLine(prefix + "desc.ArraySize = 1;");
			tw.WriteLine(prefix + "desc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;");
			tw.WriteLine(prefix + "desc.SampleDesc.Count = 1;");
			tw.WriteLine(prefix + "desc.SampleDesc.Quality = 0;");
			tw.WriteLine(prefix + "desc.Usage = D3D11_USAGE_DEFAULT;");
			tw.WriteLine(prefix + "desc.CPUAccessFlags = 0;");
			tw.WriteLine(prefix + "desc.MiscFlags = 0;");
			tw.WriteLine(prefix + "desc.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;");
			tw.Write(prefix + "unsigned int offset = ");
			tw.Write(bw.BaseStream.Position.ToString());
			tw.WriteLine(";");
			tw.WriteLine(prefix + "imageData.pSysMem = rawData + offset;");
			tw.Write(prefix + "imageData.SysMemPitch = 4 * ");
			tw.Write(sourceData.Width.ToString());
			tw.WriteLine(";");
			tw.WriteLine(prefix + "imageData.SysMemSlicePitch = 0;");


			for (int y = 0; y < sourceData.Height; ++y )
			{
				for (int x = 0; x < sourceData.Width; ++x )
				{
					Color c = sourceData.GetPixel(x, y);
					bw.Write(c.R);
					bw.Write(c.G);
					bw.Write(c.B);
					bw.Write(c.A);
				}
			}
			tw.WriteLine();
			tw.WriteLine(prefix + "HRESULT r = device->CreateTexture2D(&desc, &imageData, &m_texture);");
			tw.WriteLine(prefix + "if (r!=S_OK) { m_texture = NULL; }");
		}

		static void WriteChannel(StreamWriter tw, byte b )
		{
			tw.Write("'\\x");
			tw.Write(String.Format("{0:X2}", b));
			tw.Write("',");
		}

		static void WriteDestructor(StreamWriter tw, String name)
		{
			tw.WriteLine("\t~" + name + "()");
			tw.WriteLine("\t{");
			tw.WriteLine("\t\tif (m_texture!=NULL)");
			tw.WriteLine("\t\t{");
			tw.WriteLine("\t\t\tm_texture->Release();");
			tw.WriteLine("\t\t}");
			tw.WriteLine("\t}");
			tw.WriteLine("\tvoid Release()");
			tw.WriteLine("\t{");
			tw.WriteLine("\t\tif (m_texture!=NULL)");
			tw.WriteLine("\t\t{");
			tw.WriteLine("\t\t\tm_texture->Release();");
			tw.WriteLine("\t\t\tm_texture = NULL;");
			tw.WriteLine("\t\t}");
			tw.WriteLine("\t}");
		}

		static void WriteAccessor(StreamWriter tw)
		{
			tw.WriteLine("\tID3D11Texture2D* GetTexture() { return m_texture; }");
		}

		static void GenerateCPPTextureSwitch(String target, String source)
		{
			StreamWriter tw = new StreamWriter(target);
			String dataFile = Path.ChangeExtension(target, "dat");

			BinaryWriter bw = new BinaryWriter(File.Open(dataFile, FileMode.Create));

			String className = Path.GetFileNameWithoutExtension(target);

			tw.WriteLine("class " + className + " {");

			tw.WriteLine("\tID3D11Texture2D* m_texture;");

			WriteConstructor(tw, bw, className, source);

			WriteDestructor(tw, className);

			WriteAccessor(tw);

			tw.WriteLine("};");
			tw.Close();
			bw.Close();
		}

		static void Main(string[] args)
		{
			try
			{
				String target = args[0];
				String source = args[1];
				GenerateCPPTextureSwitch(target, source);
			}
			catch (System.Exception ex)
			{
				Console.WriteLine("CPPTextureSwitchGenerator <target> <source>");
				Console.WriteLine(ex.Message);
			}
		}
	}
}
