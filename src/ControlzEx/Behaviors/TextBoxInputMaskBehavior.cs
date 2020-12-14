#pragma warning disable CA1001

namespace ControlzEx.Behaviors
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using Microsoft.Xaml.Behaviors;

    /// <summary>
    /// Enables an InputMask for <see cref="TextBox"/> with 2 Properties: <see cref="InputMask"/>, <see cref="PromptChar"/>.
    /// </summary>
    public class TextBoxInputMaskBehavior : Behavior<TextBox>
    {
        private PropertyChangeNotifier? textPropertyNotifier;

        #region DependencyProperties

        /// <summary>Identifies the <see cref="InputMask"/> dependency property.</summary>
        public static readonly DependencyProperty InputMaskProperty = DependencyProperty.Register(nameof(InputMask), typeof(string), typeof(TextBoxInputMaskBehavior), new PropertyMetadata(string.Empty));

        public string InputMask
        {
            get { return (string) GetValue(InputMaskProperty); }
            set { SetValue(InputMaskProperty, value); }
        }

        /// <summary>Identifies the <see cref="PromptChar"/> dependency property.</summary>
        public static readonly DependencyProperty PromptCharProperty = DependencyProperty.Register(nameof(PromptChar), typeof(char), typeof(TextBoxInputMaskBehavior), new PropertyMetadata('_'));

        public char PromptChar
        {
            get { return (char) GetValue(PromptCharProperty); }
            set { SetValue(PromptCharProperty, value); }
        }

        /// <summary>Identifies the <see cref="ResetOnSpace"/> dependency property.</summary>
        public static readonly DependencyProperty ResetOnSpaceProperty = DependencyProperty.Register(nameof(ResetOnSpace), typeof(bool), typeof(TextBoxInputMaskBehavior), new PropertyMetadata(false));

        public bool ResetOnSpace
        {
            get { return (bool) GetValue(ResetOnSpaceProperty); }
            set { SetValue(ResetOnSpaceProperty, value); }
        }

        /// <summary>Identifies the <see cref="IgnoreSpace"/> dependency property.</summary>
        public static readonly DependencyProperty IgnoreSpaceProperty = DependencyProperty.Register(nameof(IgnoreSpace), typeof(bool), typeof(TextBoxInputMaskBehavior), new PropertyMetadata(true));

        public bool IgnoreSpace
        {
            get { return (bool) GetValue(IgnoreSpaceProperty); }
            set { SetValue(IgnoreSpaceProperty, value); }
        }

        #endregion

        public MaskedTextProvider? Provider { get; private set; }

        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.Loaded += AssociatedObjectLoaded;
            AssociatedObject.PreviewTextInput += AssociatedObjectPreviewTextInput;
            AssociatedObject.PreviewKeyDown += AssociatedObjectPreviewKeyDown;

            DataObject.AddPastingHandler(AssociatedObject, Pasting);
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= AssociatedObjectLoaded;
            AssociatedObject.PreviewTextInput -= AssociatedObjectPreviewTextInput;
            AssociatedObject.PreviewKeyDown -= AssociatedObjectPreviewKeyDown;

            DataObject.RemovePastingHandler(AssociatedObject, Pasting);

            textPropertyNotifier?.Dispose();

            base.OnDetaching();
        }

        /*
            Mask Character  Accepts  Required?  
            0  Digit (0-9)  Required  
            9  Digit (0-9) or space  Optional  
            #  Digit (0-9) or space  Required  
            L  Letter (a-z, A-Z)  Required  
            ?  Letter (a-z, A-Z)  Optional  
            &amp;amp;  Any character  Required  
            C  Any character  Optional  
            A  Alphanumeric (0-9, a-z, A-Z)  Required  
            a  Alphanumeric (0-9, a-z, A-Z)  Optional  
               Space separator  Required 
            .  Decimal separator  Required  
            ,  Group (thousands) separator  Required  
            :  Time separator  Required  
            /  Date separator  Required  
            $  Currency symbol  Required  

            In addition, the following characters have special meaning:

            Mask Character  Meaning  
            <  All subsequent characters are converted to lower case  
            >  All subsequent characters are converted to upper case  
            |  Terminates a previous &amp;lt; or &amp;gt;  
            \  Escape: treat the next character in the mask as literal text rather than a mask symbol  
        */
        private void AssociatedObjectLoaded(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(this.InputMask))
            {
                return;
            }

            this.Provider = new MaskedTextProvider(InputMask, CultureInfo.CurrentCulture);
            this.Provider.PromptChar = this.PromptChar;
            this.Provider.SkipLiterals = true;
            this.Provider.ResetOnSpace = this.ResetOnSpace;
            this.Provider.Set(HandleCharacterCasing(AssociatedObject.Text));

            this.AssociatedObject.AllowDrop = false;

            this.AssociatedObject.Text = GetProviderText();

            // seems the only way that the text is formatted correct, when source is updated
            // AddValueChanged for TextProperty in a weak manner
            this.textPropertyNotifier = new PropertyChangeNotifier(this.AssociatedObject, TextBox.TextProperty);
            this.textPropertyNotifier.ValueChanged += this.UpdateText;
        }

        private void AssociatedObjectPreviewTextInput(object? sender, TextCompositionEventArgs e)
        {
            if (this.Provider is null)
            {
                return;
            }

            Debug("PreviewTextInput");

            e.Handled = true;
            var text = HandleCharacterCasing(e.Text);

            this.TreatSelectedText();

            var position = this.GetNextCharacterPosition(AssociatedObject.CaretIndex);
            if (Keyboard.IsKeyToggled(Key.Insert))
            {
                if (!this.Provider.Replace(text, position))
                {
                    System.Media.SystemSounds.Beep.Play();
                    return;
                }
            }
            else
            {
                if (!this.Provider.InsertAt(text, position))
                {
                    System.Media.SystemSounds.Beep.Play();
                    return;
                }
            }

            var nextCharacterPosition = this.GetNextCharacterPosition(position + 1);
            this.RefreshText(nextCharacterPosition);
        }

        private void AssociatedObjectPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            if (this.Provider == null)
            {
                return;
            }

            // NOTE: TreatSelectedText oder sonst was nur in den IF's behandeln, weil KeyDown immer als erstes kommt
            Debug("PreviewKeyDown");

            if (e.Key == Key.Space) // handle the space
            {
                e.Handled = true;

                if (this.IgnoreSpace)
                {
                    System.Media.SystemSounds.Beep.Play();
                    return;
                }

                this.TreatSelectedText();

                var position = this.GetNextCharacterPosition(AssociatedObject.CaretIndex);
                if (!this.Provider.InsertAt(" ", position))
                {
                    System.Media.SystemSounds.Beep.Play();
                    return;
                }

                this.RefreshText(AssociatedObject.CaretIndex + 1);
            }

            if (e.Key == Key.Back) // handle the back space
            {
                e.Handled = true;

                // wenn etwas markiert war und der nutzer Backspace klickt soll nur das markierte verschwinden
                if (this.TreatSelectedText())
                {
                    this.RefreshText(AssociatedObject.CaretIndex);
                    return;
                }

                // wenn man ganz vorne steht gibs nix zu löschen, ausser wenn was selektiert war, s.h.oben
                if (AssociatedObject.CaretIndex == 0)
                {
                    return;
                }

                var denDavor = AssociatedObject.CaretIndex - 1;
                if (this.Provider.IsEditPosition(denDavor))
                {
                    if (!this.Provider.RemoveAt(denDavor))
                    {
                        System.Media.SystemSounds.Beep.Play();
                        return;
                    }
                }

                this.RefreshText(AssociatedObject.CaretIndex - 1);
            }

            if (e.Key == Key.Delete) // handle the delete key
            {
                e.Handled = true;

                // wenn etwas markiert war und der nutzer Delete klickt soll nur das markierte verschwinden
                if (this.TreatSelectedText())
                {
                    this.RefreshText(AssociatedObject.CaretIndex);
                    return;
                }

                var position = AssociatedObject.CaretIndex;
                if (this.Provider.IsEditPosition(position))
                {
                    if (!this.Provider.RemoveAt(position))
                    {
                        System.Media.SystemSounds.Beep.Play();
                        return;
                    }
                }
                else
                {
                    System.Media.SystemSounds.Beep.Play();
                    return;
                }

                this.RefreshText(AssociatedObject.CaretIndex);
            }
        }

        /// <summary>
        /// Pasting prüft ob korrekte Daten reingepastet werden
        /// </summary>
        private void Pasting(object? sender, DataObjectPastingEventArgs e)
        {
            if (this.Provider is null)
            {
                return;
            }

            // nur strg+c zulassen kein drag&drop
            if (e.DataObject.GetDataPresent(typeof(string)) && !e.IsDragDrop)
            {
                var pastedText = HandleCharacterCasing((string) e.DataObject.GetData(typeof(string)));

                this.TreatSelectedText();

                var position = GetNextCharacterPosition(AssociatedObject.CaretIndex);
                if (!this.Provider.InsertAt(pastedText, position))
                {
                    System.Media.SystemSounds.Beep.Play();
                }
                else
                {
                    this.RefreshText(position);
                    this.AssociatedObject.Focus();
                }
            }

            e.CancelCommand();
        }

        private void UpdateText(object? sender, EventArgs eventArgs)
        {
            if (this.Provider is null)
            {
                return;
            }

            Debug("UpdateText");

#pragma warning disable CA1309
            // check Provider.Text + TextBox.Text
            if (HandleCharacterCasing(this.Provider.ToDisplayString()).Equals(HandleCharacterCasing(AssociatedObject.Text)))
            {
                return;
            }
#pragma warning restore CA1309

            // use provider to format
            var success = this.Provider.Set(HandleCharacterCasing(AssociatedObject.Text));

            // ui and mvvm/codebehind should be in sync
            this.SetText(success ? GetProviderText() : HandleCharacterCasing(AssociatedObject.Text));
        }

        private string HandleCharacterCasing(string text)
        {
            switch (AssociatedObject.CharacterCasing)
            {
                case CharacterCasing.Lower:
                    return text.ToLower();
                case CharacterCasing.Upper:
                    return text.ToUpper();
                default:
                    return text;
            }
        }

        /// <summary>
        /// Falls eine Textauswahl vorliegt wird diese entsprechend behandelt.
        /// </summary>
        private bool TreatSelectedText()
        {
            if (AssociatedObject.SelectionLength > 0
                && this.Provider is not null)
            {
                this.Provider.RemoveAt(AssociatedObject.SelectionStart, AssociatedObject.SelectionStart + AssociatedObject.SelectionLength - 1);
                return true;
            }

            return false;
        }

        private void RefreshText(int position)
        {
            SetText(GetProviderText());
            Debug("SetText");
            AssociatedObject.CaretIndex = position;
        }

        private void SetText(string? text)
        {
            AssociatedObject.Text = string.IsNullOrWhiteSpace(text) ? string.Empty : text;
        }

        private int GetNextCharacterPosition(int caretIndex)
        {
            var start = caretIndex + GetAnzahlIncludeLiterals(caretIndex);
            var position = this.Provider!.FindEditPositionFrom(start, true);

            if (position == -1)
            {
                return start;
            }
            else
            {
                return position;
            }
        }

        private string? GetProviderText()
        {
            if (this.Provider is null)
            {
                return null;
            }

            // wenn noch gar kein Zeichen eingeben wurde, soll auch nix drin stehen
            // könnte man noch anpassen wenn man masken in der Oberfläche vllt doch haben will bei nem leeren feld
            return this.Provider.AssignedEditPositionCount > 0
                ? HandleCharacterCasing(this.Provider.ToDisplayString())
                : HandleCharacterCasing(this.Provider.ToString(true, true));
        }

        private int GetAnzahlIncludeLiterals(int index)
        {
            // TODO What should we do here?
            return 0; // anzLiterals;
        }

        private void Debug(string name)
        {
            System.Diagnostics.Debug.WriteLine(name + ": TextBox : " + AssociatedObject.Text);
            System.Diagnostics.Debug.WriteLine(name + ": Provider: " + Provider?.ToDisplayString());
        }
    }
}
