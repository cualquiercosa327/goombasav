﻿using Goombasav;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace goombasav_cs {
	public partial class Form1 : Form {
		const String TITLE = "Goomba Save Manager";

		private GoombaSRAM loaded_sram;
		private String filePath;
		private bool dirty;

		public Form1(String filename) {
			InitializeComponent();

			// Update status of Save and Save As items whenever File menu is opened
			fileToolStripMenuItem.DropDownOpening += (o, e) => {
				saveToolStripMenuItem.Enabled = (filePath != null && dirty);
				saveAsToolStripMenuItem.Enabled = (filePath != null);
			};
			this.Closing += (o, e) => {
				e.Cancel = !okToClose();
			};
			listBox1.SelectedIndexChanged += listBox1_SelectedIndexChanged;

			filePath = null;

			/*this->DragEnter += gcnew System::Windows::Forms::DragEventHandler(this, &goombasav_clr::MainForm::OnDragEnter);
			this->DragDrop += gcnew System::Windows::Forms::DragEventHandler(this, &goombasav_clr::MainForm::OnDragDrop);
			this->AllowDrop = true;*/

			this.DragEnter += Form1_DragEnter;
			this.DragDrop += Form1_DragDrop;
			this.AllowDrop = true;

			if (filename != null) {
				load(filename);
			}
		}

		private void openToolStripMenuItem_Click(object sender, EventArgs e) {
			OpenFileDialog d = new OpenFileDialog();
			d.Filter = "Game Boy Advance save data (*.sav)|*.sav|All files (*.*)|*.*";
			if (d.ShowDialog() == DialogResult.OK) {
				load(d.FileName);
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
			save(filePath);
		}

		private void saveAsToolStripMenuItem_Click(object sender, EventArgs e) {
			SaveFileDialog d = new SaveFileDialog();
			d.Filter = "Game Boy Advance save data (*.sav)|*.sav|All files (*.*)|*.*";
			d.AddExtension = true;
			if (d.ShowDialog() == DialogResult.OK) {
				save(d.FileName);
			}
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
			this.Close();
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e) {

		}

		private void btnReplace_Click(object sender, EventArgs e) {

		}

		private void btnExtract_Click(object sender, EventArgs e) {

		}

		private void listBox1_SelectedIndexChanged(object sender, EventArgs e) {
			GoombaHeader h = (GoombaHeader)listBox1.SelectedItem;
			lblSizeVal.Text = h.Size + " bytes";
			lblTypeVal.Text = h.Type == Stateheader.STATESAVE ? "Savestate"
				: h.Type == Stateheader.SRAMSAVE ? "SRAM"
				: h.Type == Stateheader.CONFIGSAVE ? "Config"
				: "Unknown";
			if (h is Stateheader) {
				Stateheader sh = (Stateheader)h;
				flpConfigdata.Visible = false;
				flpStateheader.Visible = true;
				lblUncompressedSize.Text =
					sh.DataSize >= sh.Size
					? "Uncompressed size:"
					: "Compressed size:";
				lblUncompressedSizeVal.Text = sh.DataSize + " bytes";
				lblFramecountVal.Text = sh.Framecount.ToString();
				lblChecksumVal.Text = sh.ROMChecksum.ToString("X8");

				panel1.Visible = true;
				uint hash = sh.CompressedDataHash();
				lblHashVal.Text = hash.ToString("X6");
				hashBox.BackColor = Color.FromArgb((int)(hash | 0xFF000000));
			} else if (h is Configdata) {
				flpConfigdata.Visible = true;
				flpStateheader.Visible = false;

				Configdata cd = (Configdata)h;
				lblBorderVal.Text = cd.BorderColor.ToString();
				lblPaletteVal.Text = cd.PaletteBank.ToString();
				MiscStrings strs = cd.GetMiscStrings;
				lblSleepVal.Text = strs.SleepStr;
				lblAutostateVal.Text = strs.AutoloadStateStr;
				lblGammaVal.Text = strs.GammaStr;
				lblChecksumVal.Text = cd.ROMChecksum.ToString("X8"); // The SRAM with this ROM checksum value is currently in 0xe000-0xffff

				panel1.Visible = false;
			} else {
				flpConfigdata.Visible = flpStateheader.Visible = panel1.Visible = false;
			}
			lblTitleVal.Text = h.Title;
		}

		private void Form1_DragEnter(object sender, DragEventArgs e) {
			throw new NotImplementedException();
		}

		private void Form1_DragDrop(object sender, DragEventArgs e) {
			throw new NotImplementedException();
		}

		private void resetDescriptionPanel() {
			btnExtract.Enabled = false;
			btnReplace.Enabled = false;
			lblSizeVal.Text = "";
			lblTypeVal.Text = "";
			flpConfigdata.Visible = false;
			flpStateheader.Visible = true;
			lblUncompressedSize.Text = "Uncompressed size:";
			lblUncompressedSizeVal.Text = "";
			lblFramecountVal.Text = "";
			lblChecksumVal.Text = "";
			lblTitleVal.Text = "";
			panel1.Visible = false;
		}

		private bool okToClose() {
			if (filePath != null && dirty) {
				DialogResult dr = MessageBox.Show("Save changes to " + Path.GetFileName(filePath) + "?",
					TITLE, MessageBoxButtons.YesNoCancel);
				if (dr == DialogResult.Yes) {
					save(filePath);
				} else if (dr == DialogResult.Cancel) {
					return false;
				}
			}
			return true;
		}

		private void load(String filename) {
			if (!okToClose()) return;
			byte[] arr = System.IO.File.ReadAllBytes(filename);
			if (arr.Length > GoombaSRAM.ExpectedSize) {
				MessageBox.Show("This file is more than " + GoombaSRAM.ExpectedSize +
					" bytes. If you overwrite the file, the last " + (arr.Length - GoombaSRAM.ExpectedSize) +
					" bytes will be discarded.", "Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			try {
				loaded_sram = new GoombaSRAM(arr, true);
			} catch (GoombaException e) {
				MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			
			filePath = filename;
			this.Text = (filename == null)
				? TITLE
				: TITLE + " - " + Path.GetFileName(filename);

			headerScan();
		}

		private void save(String path) {
			byte[] arr = loaded_sram.ToArray();
			File.WriteAllBytes(path, arr);

			filePath = path;
			dirty = false;
			this.Text = TITLE + " - " + Path.GetFileName(path);
		}

		private void headerScan() {
			listBox1.Items.Clear();
			//resetDescriptionPanel();
			listBox1.Items.AddRange(loaded_sram.Headers.ToArray());
			if (loaded_sram.Headers.Count == 0) {
				MessageBox.Show("No headers were found in this file. It may not be valid SRAM data", "Note",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information);
			}
		}
	}
}
