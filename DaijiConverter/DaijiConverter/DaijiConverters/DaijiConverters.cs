/*
The MIT License (MIT)

Copyright (c) 2016 ZZO

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

[Update History]
2016/10/27	ZZO(68B09)	First Release.
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DaijiConverters
{
	/// <summary>
	/// 大字変換クラス
	/// </summary>
	/// <remarks>
	/// ・数値(もしくは数値文字列)を"壱万弐千参百四拾五"のような大字文字列に変換します
	/// (ex)
	/// DaijiConverter daijiConv = new DaijiConverter();
	/// string daiji;
	/// daiji = daijiConv.MakeDaijiString(123456789);		// daiji="壱億弐千参百四拾五万六千七百八拾九"
	/// daiji = daijiConv.MakeDaijiString("120.3045E4");	// daiji="壱百弐拾万参千四拾五"
	/// 
	/// ・小数点以下は切り捨てるため、"0.9"等は"零"となります
	/// ・'壱'を千百十の位に付加するか否かは Append壱To千百十 フラグで決定されます
	/// ・負数の場合は文字列の先頭に'-'が付加されますが、多くの場合は負数を大字にすることはないと思われるため注意してください
	/// ・IConvertibleを実装している型(intやdouble、decimal)はMakeDaijiString()に渡せます
	/// ・IConvertibleを実装していない場合でも、指数付文字列("3.14"等)を渡すことで変換が可能です
	/// </remarks>
	public class DaijiConverter
	{
		#region 固定値
		/// <summary>
		/// 大数名称
		/// </summary>
		private readonly string[] defaultTaisuu = new string[] { "", "万", "億", "兆", "京", "垓", "𥝱", "穣", "溝", "澗", "正", "載", "極", "恒河沙", "阿僧祇", "那由他", "不可思議", };

		/// <summary>
		/// 1000、100、10、1桁の名称
		/// </summary>
		private readonly string[] defaultSenHyakuJyuu = new string[] { "千", "百", "拾", "", };

		/// <summary>
		/// 大字
		/// </summary>
		private readonly char[] defaultDaiji = new char[] { '零', '壱', '弐', '参', '四', '五', '六', '七', '八', '九', };
		#endregion

		#region フィールド・プロパティー
		/// <summary>
		/// 大数名称テーブル
		/// </summary>
		private ReadOnlyCollection<string> taisuuTbl;

		/// <summary>
		/// 大数名称テーブル取得/設定
		/// </summary>
		/// <remarks>
		/// 名称を与えるときは4桁毎の名称であること
		/// </remarks>
		public IList<string> TaisuuTbl
		{
			get
			{
				return this.taisuuTbl;
			}

			set
			{
				this.taisuuTbl = Array.AsReadOnly(value.ToArray());
			}
		}

		/// <summary>
		/// 1000、100、10、1桁の名称テーブル
		/// </summary>
		private ReadOnlyCollection<string> kanji千百拾Tbl;

		/// <summary>
		/// 1000、100、10、1桁の名称テーブル取得/設定
		/// </summary>
		/// <remarks>
		/// 名称を与える場合は[0]="千",[1]="百",[2]="十",[3]="一"の順であり、4要素以上を与えなければならないことに注意
		/// </remarks>
		public IList<string> Kanji千百拾Tbl
		{
			get
			{
				return this.kanji千百拾Tbl;
			}

			set
			{
				string[] valueArray = value.ToArray();
				if (valueArray.Length < 4) {
					throw new ArgumentException("要素数が4未満");
				}
				this.kanji千百拾Tbl = Array.AsReadOnly(valueArray);
			}
		}

		/// <summary>
		/// 大字テーブル
		/// </summary>
		private ReadOnlyCollection<char> daijiTbl;

		/// <summary>
		/// 大字テーブル取得/設定
		/// </summary>
		/// <remarks>
		/// 名称を与える場合は10要素以上を与えなければならないことに注意
		/// </remarks>
		public IList<char> KanjiNumericTbl
		{
			get
			{
				return this.daijiTbl;
			}

			set
			{
				char[] valueArray = value.ToArray();
				if (valueArray.Length < 10) {
					throw new ArgumentException("要素数が10未満");
				}
				this.daijiTbl = Array.AsReadOnly(valueArray);
			}
		}

		/// <summary>
		/// 千百十への'壱'付加フラグ
		/// </summary>
		private bool append壱To千百十 = true;

		/// <summary>
		/// 千百十への'壱'付加フラグ取得/設定
		/// </summary>
		/// <remarks>
		/// trueの場合は千百十の位にも'壱'を付加します
		/// </remarks>
		public bool Append壱To千百十
		{
			get
			{
				return this.append壱To千百十;
			}

			set
			{
				this.append壱To千百十 = value;
			}
		}

		/// <summary>
		/// 大数名称無し時の例外スロー有無フラグ
		/// </summary>
		private bool throwExceptionByTaisuuOverflow = false;

		/// <summary>
		/// 大数名称無し時のOverflowExceptionスロー有無フラグ取得/設定
		/// </summary>
		/// <remarks>
		/// trueの場合は、TaisuuTbl に名称が定義されていない時に OverflowException が発生します
		/// </remarks>
		public bool ThrowExceptionByTaisuuOverflow
		{
			get
			{
				return this.throwExceptionByTaisuuOverflow;
			}

			set
			{
				this.throwExceptionByTaisuuOverflow = value;
			}
		}
		#endregion

		#region コンストラクタ
		/// <summary>
		/// コンストラクタ
		/// </summary>
		public DaijiConverter()
		{
			this.taisuuTbl = Array.AsReadOnly(defaultTaisuu);
			this.kanji千百拾Tbl = Array.AsReadOnly(defaultSenHyakuJyuu);
			this.daijiTbl = Array.AsReadOnly(defaultDaiji);
		}
		#endregion

		#region MakeDaijiString()
		/// <summary>
		/// 大字文字列作成(数値オブジェクト受取版)
		/// </summary>
		/// <param name="pValue">変換元数値</param>
		public string MakeDaijiString(IConvertible pValue)
		{
			string strwk = ((dynamic)pValue).ToString("G35");	// 有効桁数(double=約15桁、decimal=約29桁)を全て文字列化させるために35桁としている
			return this.MakeDaijiString(strwk);
		}

		/// <summary>
		/// 大字文字列作成(数値文字列オブジェクト受取版)
		/// </summary>
		/// <param name="pValue">変換元、指数付数字文字列([-+][0-9,][.][0-9,][eE][-+][0-9])</param>
		/// <remarks>
		/// ・"31.4E-1"のような数値文字列を大字に変換する
		/// ・負数の場合は大字文字列の先頭に'-'が付加される
		/// ・該当する大数名称が無い大きな数値を与えた場合は下記２つの動作のうちいずれかとなる
		/// 1、例外を発生させる (throwExceptionByTaisuuOverflow==true)
		/// 2、大数名を付加せず文字列を作る (throwExceptionByTaisuuOverflow==false)
		/// ・千百十の位に対して'壱'を付加するか否かは append壱To千百十 フラグで決定される
		/// </remarks>
		public string MakeDaijiString(string pValue)
		{
			NumericFullString fullString = this.MakeFullNumericString(pValue);
			fullString.CutSyousuu();

			if (fullString.IsZero) {
				return daijiTbl[0].ToString();
			}

			StringBuilder result = new StringBuilder(64);

			if (fullString.IsMinus) {
				result.Append('-');
			}

			string numberStr = fullString.Seisuu;
			bool append千百十一 = false;
			int idx千百十一 = (4 - (numberStr.Length % 4)) % 4;

			int idx大数 = (numberStr.Length - 1) / 4;
			if (idx大数 >= taisuuTbl.Count) {
				if (this.throwExceptionByTaisuuOverflow) {
					throw new OverflowException("大数名が見つからないもしくは無量大数");
				}
			}

			for (int i = 0; i < numberStr.Length; i++) {
				char c = numberStr[i];
				if (c != '0') {
					int numberIdx = c - '0';
					if (idx千百十一 == 3) {
						result.Append(this.daijiTbl[numberIdx]);
						append千百十一 = true;
					} else {
						if ((numberIdx != 1) || this.append壱To千百十) {
							result.Append(this.daijiTbl[numberIdx]);
							append千百十一 = true;
						}
					}

					if (kanji千百拾Tbl[idx千百十一].Length > 0) {
						result.Append(kanji千百拾Tbl[idx千百十一]);
						append千百十一 = true;
					}
				}

				if (idx千百十一 == 3) {
					if (append千百十一) {
						if (idx大数 < taisuuTbl.Count) {
							result.Append(taisuuTbl[idx大数]);
						}
					}
				}

				idx千百十一 = (idx千百十一 + 1) % 4;
				if (idx千百十一 == 0) {
					append千百十一 = false;
					idx大数--;
				}
			}

			return result.ToString();
		}
		#endregion

		#region MakeFullNumericString()
		/// <summary>
		/// 指数無し数字文字列作成
		/// </summary>
		/// <param name="pValue">指数付数字文字列([-+][0-9,][.][0-9,][eE][-+][0-9])</param>
		/// <returns>NumericFullString</returns>
		/// <remarks>
		/// ・"31.4E-1"のような指数付数字文字列を指数なしの形(3.14)に変換する
		/// ・pValueには1以上の文字数で構成される文字列を渡さなければならない
		/// </remarks>
		public NumericFullString MakeFullNumericString(string pValue)
		{
			NumericFullString result = new NumericFullString();

			// 符号分離
			string strwk = pValue.Replace(",", "").Replace(" ", "").Trim().ToUpper();
			if (strwk.Length == 0) {
				throw new ArgumentException("pValueに有効な文字が見つからない");
			}
			result.IsMinus = (strwk[0] == '-');
			strwk = strwk.TrimStart('-', '+');

			// 仮数と指数を分離
			string[] strFields = strwk.Split('E');

			string kasuu = strFields[0];
			int sisuu;
			if (strFields.Length < 2) {
				sisuu = 0;
			} else {
				sisuu = int.Parse(strFields[1]);
			}

			// 整数部と小数部に分離
			// 整数部の先頭とと小数部の末尾の'0'を取る
			strFields = kasuu.Split('.');

			StringBuilder seisuu = new StringBuilder(64);
			seisuu.Append(strFields[0].TrimStart('0'));

			StringBuilder syousuu = new StringBuilder(64);
			if (strFields.Length < 2) {
			} else {
				syousuu.Append(strFields[1].TrimEnd('0'));
			}

			// ゼロ？
			if ((seisuu.Length == 0) && (syousuu.Length == 0)) {
				result.IsZero = true;
				return result;
			}

			if (sisuu > 0) {
				while (sisuu > 0) {
					char c;
					if (syousuu.Length == 0) {
						c = '0';
					} else {
						c = syousuu[0];
						syousuu.Remove(0, 1);
					}

					seisuu.Append(c);

					sisuu--;
				}
			} else if (sisuu < 0) {
				while (sisuu < 0) {
					char c;
					if (seisuu.Length == 0) {
						c = '0';
					} else {
						int i = seisuu.Length - 1;
						c = seisuu[i];
						seisuu.Remove(i, 1);
					}

					syousuu.Insert(0, c);

					sisuu++;
				}
			}

			result.Seisuu = seisuu.ToString().TrimStart('0');
			result.Syousuu = syousuu.ToString().TrimEnd('0');

			return result;
		}
		#endregion

		#region NumericFullString
		/// <summary>
		/// 指数無し数字文字列クラス
		/// </summary>
		public class NumericFullString
		{
			#region フィールド・プロパティー
			/// <summary>
			/// 整数部文字列
			/// </summary>
			/// <remarks>
			/// 整数部を持たないときはstring.Emptyを保持する
			/// </remarks>
			private string seisuu = "";

			/// <summary>
			/// 整数部文字列取得/設定
			/// </summary>
			/// <remarks>
			/// 整数部を持たないときはstring.Empty
			/// </remarks>
			public string Seisuu
			{
				get
				{
					return this.seisuu;
				}

				set
				{
					if (this.CheckNumericOnly(value) == false) {
						throw new ArgumentOutOfRangeException("0-9以外の文字が含まれている");
					}
					this.seisuu = value.TrimStart('0');
				}
			}

			/// <summary>
			/// 小数部文字列
			/// </summary>
			/// <remarks>
			/// 小数部を持たないときはstring.Emptyを保持する
			/// </remarks>
			private string syousuu = "";

			/// <summary>
			/// 小数部文字列取得/設定
			/// </summary>
			/// <remarks>
			/// 小数部を持たないときはstring.Empty
			/// </remarks>
			public string Syousuu
			{
				get
				{
					return this.syousuu;
				}

				set
				{
					if (this.CheckNumericOnly(value) == false) {
						throw new ArgumentOutOfRangeException("0-9以外の文字が含まれている");
					}
					this.syousuu = value.TrimEnd('0');
				}
			}

			/// <summary>
			/// マイナスフラグ取得/設定
			/// </summary>
			public bool IsMinus { get; set; }

			/// <summary>
			/// ゼロフラグ取得/設定
			/// </summary>
			public bool IsZero { get; set; }

			/// <summary>
			/// 整数部有無フラグ取得
			/// </summary>
			public bool HasSeisuu
			{
				get
				{
					return this.seisuu.Length > 0;
				}
			}

			/// <summary>
			/// 小数部有無フラグ取得
			/// </summary>
			public bool HasSyousuu
			{
				get
				{
					return this.syousuu.Length > 0;
				}
			}
			#endregion

			#region コンストラクタ
			/// <summary>
			/// コンストラクタ
			/// </summary>
			public NumericFullString()
			{
				this.IsMinus = false;
				this.IsZero = true;
			}

			/// <summary>
			/// コンストラクタ
			/// </summary>
			/// <param name="pSrc">コピー元</param>
			public NumericFullString(NumericFullString pSrc)
			{
				this.seisuu = pSrc.seisuu;
				this.syousuu = pSrc.syousuu;
				this.IsMinus = pSrc.IsMinus;
				this.IsZero = pSrc.IsZero;
			}
			#endregion

			#region SetZeroFlag()
			/// <summary>
			/// ゼロ判定
			/// </summary>
			private void SetZeroFlag()
			{
				this.IsZero = !(this.HasSeisuu || this.HasSyousuu);
				if (this.IsZero) {
					this.IsMinus = false;
				}
			}
			#endregion

			#region CheckNumericOnly()
			/// <summary>
			/// 数字文字列チェック
			/// </summary>
			/// <param name="pString">チェック対象文字列</param>
			/// <returns>true=[0-9]で構成されている、false=[0-9]以外の文字が存在する</returns>
			private bool CheckNumericOnly(string pString)
			{
				foreach (char c in pString) {
					if ((c < '0') || (c > '9')) {
						return false;
					}
				}
				return true;
			}
			#endregion

			#region CutSeisuu()
			/// <summary>
			/// 整数部削除
			/// </summary>
			/// <remarks>
			/// 整数部の削除によってゼロになる可能性があることに注意
			/// </remarks>
			public void CutSeisuu()
			{
				this.seisuu = "";
				this.SetZeroFlag();
			}
			#endregion

			#region CutSyousuu()
			/// <summary>
			/// 小数部削除
			/// </summary>
			/// <remarks>
			/// 小数部の削除によってゼロになる可能性があることに注意
			/// </remarks>
			public void CutSyousuu()
			{
				this.syousuu = "";
				this.SetZeroFlag();
			}
			#endregion

			#region ToString()
			/// <summary>
			/// 文字列化
			/// </summary>
			/// <returns>string</returns>
			/// <remarks>
			/// ・[-][0-9][[.][0-9]]形式の文字列を作成する
			/// ・小数部を持たない場合は小数点以下の文字列は作成されない
			/// ・(ex)"0","1","0.1","-1.2"
			/// </remarks>
			public override string ToString()
			{
				if (this.IsZero) {
					return "0";
				}

				StringBuilder sb = new StringBuilder(70);

				if (this.IsMinus) {
					sb.Append('-');
				}

				if (this.seisuu.Length == 0) {
					sb.Append('0');
				} else {
					sb.Append(this.seisuu);
				}

				if (this.syousuu.Length == 0) {
				} else {
					sb.Append('.');
					sb.Append(this.syousuu);
				}

				return sb.ToString();
			}
			#endregion

			#region Dump()
			/// <summary>
			/// デバッグ用ダンプ
			/// </summary>
			[Conditional("DEBUG")]
			public void Dump()
			{
				string msg = "";
				if (this.IsZero) {
					msg = "ZERO";
				} else {
					msg = this.ToString();
				}

				System.Diagnostics.Debug.WriteLine(msg);
			}
			#endregion
		}
		#endregion
	}
}
