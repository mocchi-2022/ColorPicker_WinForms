/// Color Picker
/// Copylight mocchi 2016
/// Distributed under the Boost Software License, Version 1.0.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ColorPicker {
	public partial class ColorPickerDialog : Form {
		const int CellWidth = 30, CellHeight = 25;
		public class ColorItem {
			public string Index { get; set; }
			public Color RGB {get; set;}
		};
		Label current = null;
		public ColorItem Current {
			get {
				return new ColorItem{Index = current.Text, RGB = current.BackColor};
			}
		}
		public bool EnableCustomColors {
			get {
				return tableLayoutPanel_CustomColors.Visible;
			}
			set {
				tableLayoutPanel_CustomColors.Visible = value;
				button_DefineCustomColors.Visible = value;
				label_CustomColor.Visible = value;
			}
		}

		static void ConvertRGB_to_HSL(Color rgb, out int h, out int s, out int l){
			int[] rgbv = new int[] {rgb.R, rgb.B, rgb.G};
			int max = rgbv.Max(), min = rgbv.Min();
			int dif = max - min, sum = max + min;
			int hdif = dif / 2;
			if (dif == 0){
				h = 160;
			}else if (rgb.R == max){
				if (rgb.G < rgb.B){
					h = (40 * (rgb.G - rgb.B) + hdif) / dif + 240;
				}else{
					h = (40 * (rgb.G - rgb.B) + hdif)  / dif;
				}
			}else if (rgb.G == max){
				h = (40 * (rgb.B - rgb.R) + hdif) / dif + 80;
			}else if (rgb.B == max){
				h = (40 * (rgb.R - rgb.G) + hdif) / dif + 160;
			}else h = 0; // ここには来ないはず
			if (h < 0) h += 240;
			int quot = (sum <= 255) ? sum : (510 - sum);
			s = (quot == 0) ? 0 : (dif * 240 + (quot / 2)) / quot;
			l = (sum * 120 + 127) / 255;
		}

		static void ConvertHSL_to_RGB(int h, int s, int l, out Color rgb) {
			double sd = s, ld = l;
			int l2 = (l < 120) ? (l * s + 120) / 240 : ((240 - l) * s + 120) / 240;
			int max = ((l + l2) * 255 + 120) / 240;
			int min = ((l - l2) * 255 + 120) / 240;
			int dif = max - min;
			if (h < 40) rgb = Color.FromArgb(max, (h * dif + 20) / 40 + min, min);
			else if (h < 80) rgb = Color.FromArgb(((80 - h) * dif + 20) / 40 + min, max, min);
			else if (h < 120) rgb = Color.FromArgb(min, max, ((h - 80) * dif + 20) / 40 + min);
			else if (h < 160) rgb = Color.FromArgb(min, ((160 - h) * dif + 20) / 40 + min, max);
			else if (h < 200) rgb = Color.FromArgb(((h - 160) * dif + 20) / 40 + min, min, max);
			else rgb = Color.FromArgb(max, min, ((240 - h) * dif + 20) / 40 + min);
		}

		Bitmap bmp_LumBar = new Bitmap(20, 241, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

		Panel cursor = null;
		TableLayoutPanelCellPosition lastpos_CustomColors = new TableLayoutPanelCellPosition(0, 0);
		void ChooseColor() {
			if (cursor == null) return;
			Color rgb = ((Label)cursor.Tag).BackColor;
			lock_cb = true;
			textBox_R.Text = rgb.R.ToString();
			textBox_G.Text = rgb.G.ToString();
			textBox_B.Text = rgb.B.ToString();
			lock_cb = false;
			textBox_RGB_TextChanged(null, null);
		}

		public void SetBasicColors(IEnumerable<ColorItem> colors) {
			EventHandler onclick = SetColors(tableLayoutPanel_BasicColors, colors);

			// 最初の基本色を選択
			var lst = (List<Label>)tableLayoutPanel_BasicColors.Tag;
			EventHandler onshown = (object sender, EventArgs e) => { if (lst.Count > 0) lst[0].Invoke(onclick); };
			if (this.Visible) {
				onshown(null, null);
			} else {
				this.Shown += onshown;
			}
		}

		public void SetCustomColors(IEnumerable<ColorItem> colors) {
			EventHandler onclick = SetColors(tableLayoutPanel_CustomColors, colors);
		}

		/// <summary>
		/// </summary>
		/// <param name="tableLayoutPanel"></param>
		/// <param name="colors"></param>
		/// <returns>色ラベルをクリックしたときに呼ぶデリゲートを返す。</returns>
		EventHandler SetColors(TableLayoutPanel tableLayoutPanel, IEnumerable<ColorItem> colors) {
			if (tableLayoutPanel.Tag == null) {
				tableLayoutPanel.Tag = new List<Label>();
			}
			if (cursor == null) {
				cursor = new Panel();
				cursor.Width = CellWidth;
				cursor.Height = CellHeight;
				cursor.Margin = new Padding(0);
				cursor.Padding = new Padding(0);
			}
			List<Label> collabels = (List<Label>)tableLayoutPanel.Tag;
			int count = colors.Count();
			int cols = 8;
			int rows = ((count + cols - 1) / cols);
			int num_lbl_prev = tableLayoutPanel.Controls.Count;
			if (num_lbl_prev > count) {
				for (int i = num_lbl_prev - 1; i >= count; --i) {
					tableLayoutPanel.Controls.Remove(collabels[i]);
					collabels[i].Dispose();
				}
				collabels.RemoveRange(count, num_lbl_prev - count);
			}
			tableLayoutPanel.ColumnCount = 8;
			for (int i = 0; i < tableLayoutPanel.ColumnStyles.Count; ++i) {
				tableLayoutPanel.ColumnStyles[i] = new ColumnStyle(SizeType.Absolute, CellWidth);
			}
			for (int i = tableLayoutPanel.ColumnStyles.Count; i < cols; ++i) {
				tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, CellWidth));
			}

			tableLayoutPanel.RowCount = rows;
			for (int i = 0; i < tableLayoutPanel.RowStyles.Count; ++i) {
				tableLayoutPanel.RowStyles[i] = new RowStyle(SizeType.Absolute, CellHeight);
			}
			for (int i = tableLayoutPanel.RowStyles.Count; i < rows; ++i) {
				tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, CellHeight));
			}
			if (rows < tableLayoutPanel.RowStyles.Count){
				int prevrows = tableLayoutPanel.RowStyles.Count;
				for (int i = prevrows - 1; i > rows; --i)
					tableLayoutPanel.RowStyles.RemoveAt(i);
			}
			tableLayoutPanel.Width = cols * CellWidth;
			tableLayoutPanel.Height = rows * CellHeight;

			EventHandler onclick = (object sender, EventArgs arg) => {
				this.SuspendLayout();
				// 直前のカーソルをいったん解除
				Control parent = cursor.Parent;
				if (parent != null) {
					TableLayoutPanel prevtbl = ((TableLayoutPanel)parent);
					TableLayoutPanelCellPosition prevpos = prevtbl.GetCellPosition(cursor);
					parent.Controls.Remove(cursor);
					List<Control> ctrls = new List<Control>();
					foreach (Control ct in cursor.Controls) {
						ctrls.Add(ct);
					}
					foreach (Control ct in ctrls) {
						cursor.Controls.Remove(ct);
						ct.Location = cursor.Location;
						prevtbl.Controls.Add(ct, prevpos.Column, prevpos.Row);
					}
				}
				// 新しいカーソルを設定
				Label lbl = (Label)sender;
				TableLayoutPanelCellPosition pos = tableLayoutPanel.GetCellPosition(lbl);
				if (tableLayoutPanel == tableLayoutPanel_CustomColors) {
					lastpos_CustomColors = pos;
				}
				tableLayoutPanel.Controls.Remove(lbl);
				tableLayoutPanel.Controls.Add(cursor, pos.Column, pos.Row);
				Color col = lbl.BackColor;
				cursor.BackColor = Color.Black;
				cursor.Tag = lbl;
				lbl.Location = new Point(2, 2);
				cursor.Controls.Add(lbl);
				current = lbl;
				ChooseColor();
				this.ResumeLayout();
			};
			{
				int i = 0;
				foreach (ColorItem itm in colors) {
					Label lbl = null;
					if (collabels.Count == i) {
						lbl = new Label();
						lbl.Tag = "ColorLabel";
						lbl.Width = CellWidth - 4; lbl.Height = CellHeight - 4;
						lbl.Margin = new System.Windows.Forms.Padding(2);
						lbl.AutoSize = false;
						lbl.TextAlign = ContentAlignment.MiddleCenter;
						lbl.BorderStyle = BorderStyle.Fixed3D;
						lbl.Click += onclick;
						collabels.Add(lbl);
					} else lbl = collabels[i];
					lbl.Text = itm.Index;
					lbl.BackColor = itm.RGB;
					double gry = (0.299 * (double)itm.RGB.R + 0.587 * (double)itm.RGB.G + 0.114 * (double)itm.RGB.B);
					lbl.ForeColor = (gry <= 127) ? Color.White : Color.Black;
					++i;
				}
			}

			for (int i = num_lbl_prev; i < collabels.Count; ++i) {
				int col = i & 7;
				int row = i / 8;
				tableLayoutPanel.Controls.Add(collabels[i], col, row);
			}

			return onclick;
		}
		public ColorPickerDialog() {
			InitializeComponent();

			{
				// 色パネル作成
				Color rgb;
				Bitmap bmp = new Bitmap(240, 241, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
				BitmapData bd = bmp.LockBits(new Rectangle(0, 0, 240, 241), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
				for (int j = 0, js = 0; j < 241; ++j, js += bd.Stride) {
					for (int i = 0, i3 = 0; i < 240; ++i, i3 += 3) {
						ConvertHSL_to_RGB(i, 240 - j, 120, out rgb);
						int pos = i3 + js;
						Marshal.WriteByte(bd.Scan0, pos + 0, rgb.B);
						Marshal.WriteByte(bd.Scan0, pos + 1, rgb.G);
						Marshal.WriteByte(bd.Scan0, pos + 2, rgb.R);
					}
				}
				bmp.UnlockBits(bd);
				panel_ColorPanel.BackgroundImage = bmp;

				panel_LumBar.BackgroundImage = bmp_LumBar;

				// 矢印パネル作成
				Bitmap bmp_c = new Bitmap(20, 20, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
				Graphics g = Graphics.FromImage(bmp_c);
				g.Clear(this.BackColor);
				g.FillPolygon(Brushes.Black, new Point[] { new Point(12, 0), new Point(0, 9), new Point(12, 18)});
				panel_Cursor.BackgroundImage = bmp_c;

			}

			SetColors(tableLayoutPanel_CustomColors,
				Enumerable.Range(0, 16).Select(i => new ColorItem { Index = "", RGB = Color.White })
			);

			// 画面サイズ調整
			panel_EndDialog.Location =
				new Point(panel_EndDialog.Location.X, flowLayoutPanel_Left.Bottom + panel_EndDialog.Margin.Top);
			this.ClientSize = new Size(
				flowLayoutPanel_Left.Bounds.X + flowLayoutPanel_Left.Bounds.Width,
				panel_EndDialog.Bounds.Y + panel_EndDialog.Bounds.Height);
		}

		void ChangeColor() {
			try {
				// 現在色パネルの色を変える
				Color rgb = Color.FromArgb(
					int.Parse(textBox_R.Text),
					int.Parse(textBox_G.Text),
					int.Parse(textBox_B.Text)
				);
				panel_CurrentColor.BackColor = rgb;

				// 明るさバーの色を変える
				int h = int.Parse(textBox_Hue.Text);
				int s = int.Parse(textBox_Sat.Text);

				Bitmap bmp = bmp_LumBar;
				BitmapData bd = bmp.LockBits(new Rectangle(0, 0, 20, 241), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
				for (int j = 0, js = 0; j < 241; ++j, js += bd.Stride) {
					ConvertHSL_to_RGB(h, s, 240 - j, out rgb);
					for (int i = 0, i3 = 0; i < 20; ++i, i3 += 3) {
						int pos = i3 + js;
						Marshal.WriteByte(bd.Scan0, pos + 0, rgb.B);
						Marshal.WriteByte(bd.Scan0, pos + 1, rgb.G);
						Marshal.WriteByte(bd.Scan0, pos + 2, rgb.R);
					}
				}
				bmp.UnlockBits(bd);
				panel_LumBar.Refresh();

				int l = int.Parse(textBox_Lum.Text);
				panel_Cursor.Location = new Point(panel_Cursor.Location.X, (240 - l) + panel_LumBar.Location.Y - 10);
	
			} catch {
			}
		}

		bool lock_cb = false;
		private void textBox_HSL_TextChanged(object sender, EventArgs e) {
			if (lock_cb) return;
			lock_cb = true;
			try {
				int h = int.Parse(textBox_Hue.Text);
				int s = int.Parse(textBox_Sat.Text);
				int l = int.Parse(textBox_Lum.Text);
				Color rgb;
				ConvertHSL_to_RGB(h, s, l, out rgb);
				textBox_R.Text = rgb.R.ToString();
				textBox_G.Text = rgb.G.ToString();
				textBox_B.Text = rgb.B.ToString();
			} catch {
			}
			lock_cb = false;
			ChangeColor();
		}

		private void textBox_RGB_TextChanged(object sender, EventArgs e) {
			if (lock_cb) return;
			lock_cb = true;
			try {
				Color rgb = Color.FromArgb(
					int.Parse(textBox_R.Text),
					int.Parse(textBox_G.Text),
					int.Parse(textBox_B.Text)
				);
				int h, s, l;
				ConvertRGB_to_HSL(rgb, out h, out s, out l);
				textBox_Hue.Text = h.ToString();
				textBox_Sat.Text = s.ToString();
				textBox_Lum.Text = l.ToString();
			} catch {
			}
			lock_cb = false;
			ChangeColor();
		}

		private void panel_ColorPanel_MouseDown(object sender, MouseEventArgs e) {
			Cursor.Clip = new Rectangle(panel_ColorPanel.PointToScreen(new Point(0, 0)), panel_ColorPanel.Bounds.Size);
			panel_ColorPanel_MouseMove(sender, e);
		}

		private void panel_ColorPanel_MouseMove(object sender, MouseEventArgs e) {
			if (e.Button != MouseButtons.Left) return;

			lock_cb = true;
			textBox_Hue.Text = e.X.ToString();
			textBox_Sat.Text = (240 - e.Y).ToString();
			lock_cb = false;
			textBox_HSL_TextChanged(null, null);
		}

		private void panel_Cursor_MouseDown(object sender, MouseEventArgs e) {
			Cursor.Clip = new Rectangle(panel_Cursor.PointToScreen(new Point(0, 0)), panel_Cursor.Bounds.Size);
		}

		private void panel_Cursor_MouseMove(object sender, MouseEventArgs e) {
			if (e.Button != MouseButtons.Left) {
				panel_Clip_MouseUp(sender, e);
				return;
			}
			Point pt_sc = ((Panel)sender).PointToScreen(new Point(e.X, e.Y));
			Point pt_lb = panel_LumBar.PointToClient(pt_sc);
			if (pt_lb.Y < 0) pt_lb.Y = 0;
			else if (pt_lb.Y > 240) pt_lb.Y = 240;
			int l = 240 - pt_lb.Y;
			panel_Cursor.Location = new Point(panel_Cursor.Location.X, pt_lb.Y + panel_LumBar.Location.Y - 10);
			Cursor.Clip = new Rectangle(panel_Cursor.PointToScreen(new Point(0, 0)), panel_Cursor.Bounds.Size);
			textBox_Lum.Text = l.ToString();
		}

		private void panel_Clip_MouseUp(object sender, MouseEventArgs e) {
			Cursor.Clip = Rectangle.Empty;
		}

		private void button_DefineCustomColors_Click(object sender, EventArgs e) {
			button_DefineCustomColors.Enabled = false;

			// 画面サイズ調整
			panel_EndDialog.Location = new Point(panel_EndDialog.Location.X,
				Math.Max(flowLayoutPanel_Left.Bottom, panel_Right.Bottom - panel_EndDialog.Bounds.Height) + panel_EndDialog.Margin.Top);

			this.ClientSize = new Size(
				panel_Right.Bounds.X + panel_Right.Bounds.Width,
				panel_EndDialog.Bounds.Y + panel_EndDialog.Bounds.Height);

		}

		private void button_AddToCustomColors_Click(object sender, EventArgs e) {
			Control ctrl = tableLayoutPanel_CustomColors.GetControlFromPosition(lastpos_CustomColors.Column, lastpos_CustomColors.Row);
			Label clbl = null;
			if (ctrl.Tag == null) return;
			else if (ctrl.Tag.GetType() == typeof(string) && ((string)ctrl.Tag) == "ColorLabel") clbl = (Label)ctrl;
			else if (ctrl.Tag.GetType() == typeof(Control) &&
				(((Control)ctrl.Tag).Tag.GetType() == typeof(string) && ((string)((Control)ctrl.Tag).Tag) == "ColorLabel"))
					clbl = (Label)((Control)ctrl.Tag).Tag;
			if (clbl == null) return;
			clbl.BackColor = panel_CurrentColor.BackColor;
			current = clbl;
			lastpos_CustomColors.Row++;
			if (lastpos_CustomColors.Row >= tableLayoutPanel_CustomColors.RowCount) {
				lastpos_CustomColors.Row = 0;
				lastpos_CustomColors.Column++;
				if (lastpos_CustomColors.Column >= tableLayoutPanel_CustomColors.ColumnCount) {
					lastpos_CustomColors.Column = 0;
				}
			}
		}

		private void button_End_Click(object sender, EventArgs e) {
			DialogResult = ((Button)sender).DialogResult;
			Close();
		}
	}
}
