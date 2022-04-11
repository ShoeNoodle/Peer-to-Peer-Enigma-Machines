using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Enigma_Emulator
{
    class EnigmaMachine
    {
        // Enigma I
        //dictionary for plugboard contains 2 chars
        private Dictionary<Char, Char> plugBoard;
        //creating variable for all rtrs, reflectors and the alphabet.
        private Rotor[] rtrs;
        private Rotor reflector;
        private const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string rtrI = "EKMFLGDQVZNTOWYHXUSPAIBRCJ";
        private const string rtrII = "AJDKSIRUXBLHWTMCQGZNPYFVOE";
        private const string rtrIII = "BDFHJLCPRTXVZNYEIWGAKMUSQO";
        private const string refA = "EJMZALYXVBWFCRQUONTSPIKHGD";
        private const string refB = "YRUHQSLDPXNGOKMIEBFZCWVJAT";
        private const string refC = "FVPJIAOYEDRZXWGCTKUQSBNMHL";

        private class Rotor
        {
            // The current notch that the rtr is on
            private int outertrpositionition;
            public char outerChar { get; set; }
            private string wiring;
            private char turnOver;
            public string name { get; }
            public char ring { get; set; }
            public int[] map { get; }
            public int[] revMap { get; }


            public Rotor(string wire, char turnov, string n)
            {
                turnOver = turnov;
                outertrpositionition = 0;

                ring = 'A'; 
                name = n;

                map = new int[26];
                revMap = new int[26];

                setWiring(wire);
            }
            //programming the wiring by looping around the rtr alphabet
            public void setWiring(string newWire)
            {
                wiring = newWire;
                outerChar = wiring.ToCharArray()[outertrpositionition];
                for (int i = 0; i < 26; i++)
                {
                    int match = ((int)wiring.ToCharArray()[i]) - 65;
                    map[i] = (26 + match - i) % 26;
                    revMap[match] = (26 + i - match) % 26;
                }
            }

            public int getOuterotorpositionition()
            {
                return outertrpositionition;
            }

            public void setOuterChar(char c)
            {
                outerChar = c;
                outertrpositionition = alphabet.IndexOf(outerChar);
            }

            public void step()
            {
                outertrpositionition = (outertrpositionition + 1) % 26;
                outerChar = alphabet.ToCharArray()[outertrpositionition];
            }

            public bool isInTurnOver()
            {
                return outerChar == turnOver;
            }
        }
        //rotates rotors by stepping and incrementing if rtr 2 or 3 is going to turnover
        private void rtrRotation(Rotor[] rtr)
        {
            //steps all rtrs
            if (rtr.Length == 3)
            {
                if (rtr[1].isInTurnOver())
                {
                    rtr[0].step();
                    rtr[1].step();
                }
                //steps middle rtr
                else if (rtr[2].isInTurnOver())
                {
                    rtr[1].step();
                }
                rtr[2].step();
            }
        }
        private char rtrMap(char c, bool reverse)
        {
            int characterposition = (int)c - 65;
            if (!reverse)
            {
                for (int i = rtrs.Length - 1; i >= 0; i--)
                {
                    characterposition = rtrValue(rtrs[i], characterposition, reverse);
                }
            }
            else
            {
                for (int i = 0; i < rtrs.Length; i++)
                {
                    characterposition = rtrValue(rtrs[i], characterposition, reverse);
                }
            }

            return alphabet.ToCharArray()[characterposition];
        }

        private int rtrValue(Rotor r, int characterposition, bool reverse)
        {
            int rtrposition = (int)r.ring - 65;
            int d;
            if (!reverse)
                d = r.map[(26 + characterposition + r.getOuterotorpositionition() - rtrposition) % 26];
            else
                d = r.revMap[(26 + characterposition + r.getOuterotorpositionition() - rtrposition) % 26];

            return (characterposition + d) % 26;
        }

        private char reflectorMap(char c)
        {
            int characterposition = (int)c - 65;
            characterposition = (characterposition + reflector.map[characterposition]) % 26;
            return alphabet.ToCharArray()[characterposition];
        }
        public void setReflector(char conf)
        {
            conf = char.ToUpper(conf);
            string wiring = "";
            switch (conf)
            {
                case 'A':
                    wiring = refA;
                    break;
                case 'B':
                    wiring = refB;
                    break;
                case 'C':
                    wiring = refC;
                    break;
            }
            reflector.setWiring(wiring);
        }
        public EnigmaMachine()
        {
            plugBoard = new Dictionary<char, char>();

            Rotor rI = new Rotor(rtrI, 'Q', "I");
            Rotor rII = new Rotor(rtrII, 'E', "II");
            Rotor rIII = new Rotor(rtrIII, 'V', "III");
            rtrs = new Rotor[] { rI, rII, rIII }; 
            reflector = new Rotor(refA, ' ', "");
        }


        // Enter the ring settings and initial rtr positions
        public void setSettings(char[] rings, char[] ground)
        {
            for (int i = 0; i < rtrs.Length; i++)
            {
                rtrs[i].ring = Char.ToUpper(rings[i]);
                rtrs[i].setOuterChar(Char.ToUpper(ground[i]));
            }
        }

        public void setSettings(char[] rings, char[] ground, string rtrOrder)
        {
            Rotor rI = null;
            Rotor rII = null;
            Rotor rIII = null;

            // Get the current ordering
            for (int i = 0; i < rtrs.Length; i++)
            {
                if (rtrs[i].name == "I")
                    rI = rtrs[i];
                if (rtrs[i].name == "II")
                    rII = rtrs[i];
                if (rtrs[i].name == "III")
                    rIII = rtrs[i];
            }

            string[] order = rtrOrder.Split('-');

            // Set the new ordering
            for (int i = 0; i < order.Length; i++)
            {
                if (order[i] == "I")
                    rtrs[i] = rI;
                if (order[i] == "II")
                    rtrs[i] = rII;
                if (order[i] == "III")
                    rtrs[i] = rIII;
            }

            setSettings(rings, ground);
        }

        public void setSettings(char[] rings, char[] ground, string rtrOrder, char reflector)
        {
            setReflector(reflector);
            setSettings(rings, ground, rtrOrder);
        }

        // Encrypts or decrypts a message
        public string runEnigma(string message)
        {
            StringBuilder fullMessage = new StringBuilder();

            message = message.ToUpper();

            foreach (char c in message)
            {
                fullMessage.Append(encryptCharacter(c));
            }

            return fullMessage.ToString();
        }


        private char encryptCharacter(char character)
        {

            // rtrs are rotated before character is passed
            rtrRotation(rtrs);

            if (plugBoard.ContainsKey(character))
            {
                character = plugBoard[character];
            }

            //Characteer is sent through the rtrmap
            character = rtrMap(character, false);

            //character is sent through the reflector
            character = reflectorMap(character);

            // the character is then reflected through the rtrs again
            character = rtrMap(character, true);

            if (plugBoard.ContainsKey(character))
            {
                character = plugBoard[character];
            }

            // Character is now encrypted
            return character;
        }

        // Add a character pair into the plugboard
        public void addPlug(char char1, char char2)
        {
            if (Char.IsLetter(char1) && Char.IsLetter(char2))
            {
                //puts chars to upper
                char1 = Char.ToUpper(char1);
                char2 = Char.ToUpper(char2);
                if (char1 != char2 && !plugBoard.ContainsKey(char1))
                {
                    plugBoard.Add(char1, char2);
                    plugBoard.Add(char2, char1);
                }
                //chars are stored both ways as to swap if it is A or S or S or A
                if (char2 != char1 && !plugBoard.ContainsKey(char2))
                {
                    plugBoard.Add(char1, char2);
                    plugBoard.Add(char2, char1);
                }
            }
        }
    }
}