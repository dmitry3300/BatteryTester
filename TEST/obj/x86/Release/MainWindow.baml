   M S B A M L   `   `   `  ���� U  QMahApps.Metro, Version=1.5.0.23, Culture=neutral, PublicKeyToken=f4fb5a3c4d1e5b4fV;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.MetroMahApps.Metro.Controls  ? ;TEST, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullclr-namespace:TESTTEST    TEST.MainWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    NControls;clr-namespace:MahApps.Metro.Controls;assembly=MahApps.Metro    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     Cd34http://research.microsoft.com/DynamicDataDisplay/1.0     $localclr-namespace:TEST     -   (   "MahApps.Metro.Controls.MetroWindow   Name$  _this�� � Title$ Battery tester��$	��749�� � Icon$ battery loading.ico�� � 
ResizeMode$ CanMinimize=� � SizeToContent$ Height=� � 	Resources  �� MergedDictionaries �� Source  TEST.PwmConverter   MyPwmConverter  TEST.PwmHintConverter  MyPwmHintConverter  TEST.CapacityConverter  MyCapacityConverter��  �� $G Apack://application:,,,/MahApps.Metro;component/Styles/Colors.xaml?��� $M Gpack://application:,,,/MahApps.Metro;component/Styles/Accents/Blue.xaml?��� $R Lpack://application:,,,/MahApps.Metro;component/Styles/Accents/BaseLight.xaml?�%-   &	        &	      &	 
           .��� .+��� .@��� $��Батареи��.���� .+��  � ColumnDefinitions �� $��1* ��� $��1* ��� $��1* ��� $��75 �.+��� -   $��	linkLabel��$��16(�$��2��$��0��$��0q�
���   �$��Center=����� 	 �� 
TargetType"	 ��.��� 
 �� Property
 � �� �� Value$) #Связь с блоком есть���� 
 � ��
 � � � �� Triggers ��  �� Binding ��  �� Path$ 	LinkState� �� UpdateSourceTrigger$ PropertyChanged=� �� Value	 False.a��� 
 � ��
 �  ���� 
 � ��
 � ����� 
 � ��$* $Связи с блоком нет!!!���� -   $��comPortCombo�� �� SelectedValuePath$	 Key�� �� DisplayMemberPath$ Value��$��2�� �� SelectedIndex$ 0��$��1,1,5,1q����� *	PortNames+�� -   $��enableAudioCB��$��3��$��Center=�$��Center=� d� 	IsChecked �� *AudioEnable+.��Audio�� $��Каналы��.���  � RowDefinitions �� $
��0.5* ��� $��8* �.+�� $��0��$
��20,0q� �� $��1* ��� $��1* ��� $��1* ��� $��1* ��� $��1* ��� $��1* ��� $��1* ��� $��1* �.+��� $��	Напр.��$��0��$��Center=��� $��Тип АКБ��$��1��$��Center=��� $��Ёмкость��$��2��$��Center=��� $��
Время��$��3��$��Center=��� $��
Режим��$��4��$��Center=��� -   $��channelsList��$��1��$��Stretch=�$��1,0,1,1q����� *BatteryCannels+ �� SelectedItem �� *SelectedBattery+��Z� ���� .R��� $��infoGroupBox��$��1������ $ BatNameProp�.���  �� $��1* ��� $��1* ��� $��1* ��� $��1* ��� $��1* ��� $��1* ��� $��1* ��� $��1* �.+��� $��0��.+��� $��SLabel�� �� ContentStringFormat$ 	{0:0.0} V������ $ voltageProp� �� Mode$ OneWay=��  � Maximum$	 255Q� � LargeChange$ 0Q�$��10�����	#FF00FF2F � Minimum$ 0Q� � Value �� $ PwmProp� �� 	Converter# [  $ OneWay=� � ToolTip �� $ PwmProp�# [ $ OneWay=��� $��TLabel��$��1������ $ typePropText�$ OneWay=��� $��CLabel��$ {0:0.00} Ah��$��2������ "	 ����� 
 � �� �� $ capacityProp�# [ $ OneWay=� ��  �� $ modeProp�$ PropertyChanged=� AUTO_CHARGE.a��� 
 � ��$	 ---����  �� $ modeProp�$ PropertyChanged=�
 CHARGE.a��� 
 � ��$	 ---����  �� $ modeProp�$ PropertyChanged=�	 READY.a��� 
 � ��
 � �����  �� $ modeProp�$ PropertyChanged=� 
AUTO_READY.a��� 
 � ��
 � ����� $��VLabel��$��3������ $ timeProp�$ OneWay=��� $
��mode��$��4������ $ modePropText�$ OneWay=����� "	 �� ��  �� $ modeProp�$ PropertyChanged=� AUTO_CHARGE.a��� 
 � �� �	#FFF1F18C��  �� $ modeProp�$ PropertyChanged=�
 CHARGE.a��� 
 � �� �	#FFF1F18C��  �� $ modeProp�$ PropertyChanged=� AUTO_DISCHARGE.a��� 
 � �� �	#FF89E289��  �� $ modeProp�$ PropertyChanged=� 	DISCHARGE.a��� 
 � �� �	#FF89E289��  �� $ modeProp�$ PropertyChanged=� AUTO_RECHARGE.a��� 
 � �� �	#FFF0A6F0�� $��	modeCombo��$��5��$	 Key��$ Value��$ 1��$��Center=� � DataContext ��   �� ElementName$  channelsList��$ DataContext����� *AvailableModes+���� "	 �� ��  �� $  	modeCombo��$ SelectedValue� FULL_AUTO_STANDBY.a��� 
 �@�ToolTip$P JПредварительный разряд акб перед тестом���� $��6�� �� $  channelsList��$ DataContext����� $ ChangeModeCommand����� $  	modeCombo��$ SelectedValue�.��Set�� $��7��
���  ��  TEST.Channel_ModeState!   READY#��Z!  �� $  channelsList��$ DataContext����� $ ChangeModeCommand�.��STOP " P� Property# P� Value��%d   '��       �� "	 �� P� " �@J�IsKeyboardFocusWithin# . . ��� 
 �@��
IsSelected . �� $	��LOG��$	��200��$��Bottom=�$��1,1,0,1q�$��0,0,0,20q�.���� -   $��LogList��$��Bottom=�$��Visible=���.  $��0,0,0,20q����� *Log+�� $
��Graf��$y�Hidden=�