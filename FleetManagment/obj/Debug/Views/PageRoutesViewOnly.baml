   M S B A M L   `   `   `  ����
  Zachet%    Zachet.Views.PageRoutesViewOnly   5      R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Aav2http://schemas.microsoft.com/expression/blend/2008     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5        N� Title$  PageRoutesViewOnly��5      .��5      � ���#F0F0FF$��Stretch=�6    $��Stretch=�6>    � RowDefinitions 5   
   �� 5      $	��100 �6   �� 5      $��* �6   .+�5   
   �� $%��Список маршрутов��6   $��24(�5      $
��Bold"�6"   ���#6A0DAD$��Center=�5      $	��Top=�61   $��0,10,0,0q�6I   � 5   
   $
��Left=�6   $��Center=�6*   $��20,0,0,0q�6E    5      �� 5      $
��Auto �6    �� 5      $
��Auto �6     � ColumnDefinitions 5      �� 5      $
��Auto �6#   �� 5      $
��Auto �6#   �� 5      $
��Auto �6#   .+�5!      �� $��0��6   $��0��6%   $"��Начальная дата:��65   $��Center=�6L   $	��100��6g   $��0,0,5,5q�6s   (  "System.Windows.Controls.DatePicker  5"      -   5          Name$ StartDatePicker��5"      $��0��62   $��1��6?   $	��120��6O   $��25��6[   $��Center=�6g   $��0,0,5,5q�6�   �� 5#      -   5        $��0��5#   6   $��2��6C   $��2��6S   $��25��5$      $��80��6!   ���#20B2AA
�������$
��Bold"�5&      ���#483D8B$��2q�6=   $	��5,2q�5'       � Cursor$
 Hand��6#   $��Center=�61   $��Фильтр��6L   �� 5)      $��1��6   $��0��6%   $ ��Конечная дата:��65   $��Center=�6K   $	��100��6f   $��0,0,5,0q�6r     5*      -   5        $ EndDatePicker��5*      $��1��60   $��1��6=   $	��120��6M   $��25��6Y   $��Center=�6e   $��0,0,5,0q�6�   5+      �� 5.   
    �� Orientation$ 
Horizontal=�6   $��Right=�6.   $��Center=�6J   $��0,0,20,0q�6e   .+�5/      �� -   5        $��40��50      $	��100��6!   ���#FF6F61
�������$
��Bold"�52      ���#483D8B$��2q�6=   $
��10,5q�53      $��0,0,10,0q�6$   $
 Hand��66   $��
Назад��6D   54      &   System.Windows.Controls.DataGrid  56   
   -   5        $��
RoutesGrid��58      $��1��56         
IsReadOnly .    CanUserAddRows .     CanUserDeleteRows .  	   AutoGenerateColumns	 .  
�������
   AlternatingRowBackground
 �#EEEAF8���#6A5ACD$��2q�5:   L   $��20,10,20,20q�5;      $��Stretch=�6(   $��Stretch=�6F      Columns 5<      0  *System.Windows.Controls.DataGridTextColumn  5=      ,  &System.Windows.Controls.DataGridColumn   Header$ Id��6%      Width5  /System.Windows.Controls.DataGridLengthConverter$ * 6H   1  +System.Windows.Controls.DataGridBoundColumn   Binding 61   �� *Id+6H     5>      $ Водитель��6%   $ * 6\    67   �� *Drivers.FullName+6\     5?      $ Транспорт��6%   $ * 6[    68   �� *Vehicles.Model+6[     5@      $ Дата начала��6%   $ * 6X    6:   �� *	StartDate+6X     5A      $! Дата окончания��6%   $ * 6Y    6=   �� *	EndDate+6Y     5B      $5 /Начальное местоположение��6%   $ * 6i    6G   �� *StartLocation+6i     5C      $3 -Конечное местоположение��6%   $ * 6f    6F   �� *EndLocation+6f     5D      $! Расстояние (км)��6%   $ * 6[    6>   �� *
Distance+6[     5E      $ Статус��6%   $ * 6W    65   �� *VehicleStatus+6W   5G      5H      5I      