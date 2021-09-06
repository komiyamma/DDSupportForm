using System;
using System.IO;
using System.Windows.Forms;

using ImageProcessor;
using ImageProcessor.Imaging.Formats;

namespace DDSupportedForm
{
    partial class DDForm
    {
        // フォームのイベントハンドラ初期化
        private void init_form()
        {

            //DragEnterイベントハンドラを追加
            this.DragEnter += new DragEventHandler(form_DragEnter);

            //DragDropイベントハンドラを追加
            this.DragDrop += new DragEventHandler(form_DragDrop);

        }

        // 保存できるかできないかは簡単には判断できないので、とりあえずコピーできる前提でアイコン変更
        private void form_DragEnter(object sender, DragEventArgs e)
        {
            string url = e.Data.GetData(DataFormats.Text).ToString();
            e.Effect = DragDropEffects.Copy;
        }

        // サポートしている場合は解釈して、webpフォーマットに変換して保存。
        // サポートしていない場合はMemoryStreamの内容を信じてそのまま拡張子だけ.webpとして保存
        private void form_DragDrop(object sender, DragEventArgs e)
        {
            var fileNames = e.Data.GetFileContentNames();
            for (int i = 0; i < fileNames.Length; i++)
            {
                bool success = false;
                string filename = "";
                using (var ms = e.Data.GetFileContent(i))
                {
                    if (!success)
                    {
                        (success, filename) = TrySaveSupportTypeContent(ms);
                    }

                    if (!success)
                    {
                        (success, filename) = TrySaveUnsupportTypeContent(ms);
                    }

                    MessageBox.Show(filename);
                }
            }
        }

        // ファイルの名前を決める。とりあえずかぶらないようにUTCから生成
        private static string MakeFileName()
        {
            // ファイルの名前はUTCから生成。(適当仕様)
            DateTime localNow = DateTime.Now;
            long utc = localNow.ToFileTimeUtc();
            return utc.ToString();
        }


        // このプログラムで利用しているライブラリで解釈が「可能」なファイルを対象とした保存。bmp, jpg, png, webp, gifなど
        private static (bool, string) TrySaveSupportTypeContent(MemoryStream ms)
        {
            try
            {
                ImageFactory imgfactory = new ImageFactory();
                imgfactory.Load(ms);
                // メモリストリームの内容はImageFactoryがサポートしている内容？
                ISupportedImageFormat format = FormatUtilities.GetFormat(ms);
                string ext = format.DefaultExtension;

                string save_base = MakeFileName();
                string save_filename = save_base + '.' + ext;
                imgfactory.Format(format).Save(save_filename);
                return (true, save_filename);
            }
            catch (Exception)
            {
            }

            return (false, "");
        }

        // このプログラムで利用しているライブラリで解釈が「不可能」なファイルを対象とした保存。ほとんどの場合 webp のアニメーションタイプ
        private static (bool, string) TrySaveUnsupportTypeContent(MemoryStream ms)
        {
            try
            {

                string save_base = MakeFileName();
                string save_filename = save_base + '.' + "webp";
                using (FileStream file = new FileStream(save_filename, FileMode.Create, System.IO.FileAccess.Write))
                {
                    byte[] bytes = new byte[ms.Length];
                    ms.Read(bytes, 0, (int)ms.Length);
                    file.Write(bytes, 0, bytes.Length);
                    return (true, save_filename);
                }
            }
            catch (Exception)
            {
            }

            return (false, "");
        }
    }
}

