#region Using declarations
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NinjaTrader.Cbi;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.Strategies;
using NinjaTrader.NinjaScript.Indicators;
#endregion

namespace NinjaTrader.NinjaScript.Strategies
{
    public class PhoenixORBPro : Strategy
    {
        private EMA ema200;
        private ATR atr;
        private double openingHigh, openingLow;
        private bool rangeBuilt, tradeTaken;

        [NinjaScriptProperty]
        [Range(1,10)]
        public double RiskReward { get; set; }

        [NinjaScriptProperty]
        [Range(5,60)]
        public int OpeningRangeMinutes { get; set; }

        protected override void OnStateChange()
        {
            if(State == State.SetDefaults)
            {
                Name="PhoenixORBPro";
                Description="Phoenix ORB Pro Base";
                Calculate=Calculate.OnBarClose;
                EntriesPerDirection=1;
                IsExitOnSessionCloseStrategy=true;
                ExitOnSessionCloseSeconds=30;
                BarsRequiredToTrade=200;
                RiskReward=2;
                OpeningRangeMinutes=15;
            }
            else if(State==State.DataLoaded)
            {
                ema200=EMA(200);
                atr=ATR(14);
            }
        }

        protected override void OnBarUpdate()
        {
            if(CurrentBar<BarsRequiredToTrade)
                return;

            if(Bars.IsFirstBarOfSession)
            {
                openingHigh=double.MinValue;
                openingLow=double.MaxValue;
                rangeBuilt=false;
                tradeTaken=false;
            }

            int barsFromOpen = Bars.BarsSinceSession;

            if(!rangeBuilt)
            {
                if(barsFromOpen < OpeningRangeMinutes)
                {
                    openingHigh=Math.Max(openingHigh,High[0]);
                    openingLow=Math.Min(openingLow,Low[0]);
                    return;
                }
                rangeBuilt=true;
            }

            if(tradeTaken)
                return;

            double range=openingHigh-openingLow;
            if(range<=TickSize*8) return;

            bool longOk=Close[0]>ema200[0];
            bool shortOk=Close[0]<ema200[0];

            if(longOk && Close[0]>openingHigh+2*TickSize)
            {
                SetStopLoss(CalculationMode.Price,openingLow);
                SetProfitTarget(CalculationMode.Price,Close[0]+range*RiskReward);
                EnterLong("ORB_LONG");
                tradeTaken=true;
            }
            else if(shortOk && Close[0]<openingLow-2*TickSize)
            {
                SetStopLoss(CalculationMode.Price,openingHigh);
                SetProfitTarget(CalculationMode.Price,Close[0]-range*RiskReward);
                EnterShort("ORB_SHORT");
                tradeTaken=true;
            }
        }
    }
}
