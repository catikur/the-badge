using NUnit.Framework;
using TheBadge.Greybox.Loop;
using TheBadge.Greybox.Sim;
using TheBadge.Sim.Commands;
using UnityEngine;

namespace TheBadge.Greybox.Tests
{
    /// <summary>Tycoon mini-modeli (GDD 4.2) ve Tek Kapı hafif doğrulaması testleri.</summary>
    public class EconomyAndBusTests
    {
        static GreyboxBalance Bal()
        {
            var txt = Resources.Load<TextAsset>("greybox.balance");
            Assert.IsNotNull(txt);
            return JsonUtility.FromJson<GreyboxBalance>(txt.text);
        }

        [Test]
        public void FiyatArttikcaDolulukDuser()
        {
            var eco = Bal().ekonomi;
            float occLow = TycoonEconomy.Occupancy(eco, eco.refFiyat * 0.5f, new int[0]);
            float occRef = TycoonEconomy.Occupancy(eco, eco.refFiyat, new int[0]);
            float occHigh = TycoonEconomy.Occupancy(eco, eco.refFiyat * 1.8f, new int[0]);
            Assert.Greater(occLow, occRef);
            Assert.Greater(occRef, occHigh);
            Assert.That(occRef, Is.EqualTo(eco.talepTaban).Within(1e-3f), "ref fiyatta nötr talep");
        }

        [Test]
        public void FormTalebiEtkiler()
        {
            var eco = Bal().ekonomi;
            float kotu = TycoonEconomy.Occupancy(eco, eco.refFiyat, new[] { -1, -1, -1, -1, -1 });
            float iyi = TycoonEconomy.Occupancy(eco, eco.refFiyat, new[] { 1, 1, 1, 1, 1 });
            Assert.Greater(iyi, kotu, "galibiyet serisi doluluk getirmeli (GDD 4.2)");
        }

        [Test]
        public void DolulukBantlariClamplanir()
        {
            var eco = Bal().ekonomi;
            Assert.That(TycoonEconomy.Occupancy(eco, eco.fiyatMax, new[] { -1, -1, -1, -1, -1 }),
                Is.GreaterThanOrEqualTo(eco.dolulukMin));
            Assert.That(TycoonEconomy.Occupancy(eco, eco.fiyatMin, new[] { 1, 1, 1, 1, 1 }),
                Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void BusBantDisiFiyatiReddeder()
        {
            var bal = Bal();
            var st = GreyboxState.NewGame(bal);
            var bus = new GreyboxCommandBus(bal, st);
            float before = st.ticketPrice;

            var r = bus.Send(GreyboxCommandBus.ActSetTicketPrice, GreyboxJson.Payload("price", bal.ekonomi.fiyatMax + 10));
            Assert.AreEqual(RejectionReason.ParamOutOfBand, r);
            Assert.AreEqual(before, st.ticketPrice, "reddedilen komut durumu DEĞİŞTİREMEZ");
        }

        [Test]
        public void BusBilinmeyenAksiyonuReddeder()
        {
            var bal = Bal();
            var st = GreyboxState.NewGame(bal);
            var bus = new GreyboxCommandBus(bal, st);
            Assert.AreEqual(RejectionReason.UnknownAction,
                bus.Send("greybox.hack_money", GreyboxJson.Payload("amount", 1000000)));
        }

        [Test]
        public void BusGecerliKomutlariUygular()
        {
            var bal = Bal();
            var st = GreyboxState.NewGame(bal);
            var bus = new GreyboxCommandBus(bal, st);

            Assert.AreEqual(RejectionReason.None,
                bus.Send(GreyboxCommandBus.ActSetTicketPrice, GreyboxJson.Payload("price", 26)));
            Assert.AreEqual(26f, st.ticketPrice, 1e-4f);

            Assert.AreEqual(RejectionReason.None,
                bus.Send(GreyboxCommandBus.ActSelectTactic, GreyboxJson.Payload("tacticId", 2)));
            Assert.AreEqual(2, st.tacticId);

            Assert.AreEqual(RejectionReason.None, bus.Send(GreyboxCommandBus.ActNextMatch, null));
            Assert.AreEqual(2, st.matchIndex);
        }

        [Test]
        public void SettleFormPenceresiSon5iTutar()
        {
            var bal = Bal();
            var st = GreyboxState.NewGame(bal);
            long before = st.money;
            st.Settle(bal, 1); st.Settle(bal, 1); st.Settle(bal, -1);
            st.Settle(bal, 0); st.Settle(bal, 1); st.Settle(bal, 1);
            Assert.AreEqual(5, st.lastResults.Length);
            CollectionAssert.AreEqual(new[] { 1, -1, 0, 1, 1 }, st.lastResults);
            Assert.Greater(st.money, before, "gelir kasaya işlenmeli");
        }
    }
}
