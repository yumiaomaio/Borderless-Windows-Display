   M S B A M L   `   `   `  ����  BorderlessWindowApp*    $BorderlessWindowApp.Views.MainWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	     � Title$  Borderless Window Tool��5   	   $	��400��6(   $	��500��65    � WindowStartupLocation$ CenterScreen=�5   	   .��5      � $��20q�6    � RowDefinitions 5   
   �� 5	      $
��Auto �6   �� 5
      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   .+�5   
   �� $��0��6   $��窗口标题：��6!   $
��Bold"�6.   $��0,0,0,5q�6@   �� 5   
   -   5         ��Name$ txtWindowTitle��5      $��1��6*   $��30��67   $��0,0,0,10q�6C   ��5      �� *WindowTitle+ �� UpdateSourceTrigger$ PropertyChanged=��� 5   
   $��2��6    �� Orientation$ 
Horizontal=�6"   $	��Top=�6;   $��0,5,0,0q�6S   .+�5      �� $��	宽度：��6   $��Center=�6#   $��0,0,5,0q�6>   �� 5      -   5        $ txtWidth��5      $��60��6(   $��0,0,10,0q�63   �� 5      $��	高度：��6   $��Center=�6#   $��0,0,5,0q�6>   �� 5      -   5        $ 	txtHeight��5      $��60��6)   $��0,0,10,0q�64   �� 5      -   5        $��设置大小��5      $��80��6$   6/   5      �� 5"   
   $��3��6   $ 
Horizontal=�6"   $	��Top=�6;   $��0,10,0,0q�6S   .+�5#      �� -   5        $��查找窗口��5#      $	��100��6$   $��0,0,10,0q�6J   �� 5$      -   5         ��Name$ comboWindows��5$      $	��250��6-   69   5%      �� 5(   
   $��4��6   $ 
Horizontal=�6"   $��Right=�6;   $��	0,20,0,10q�6W   .+�5)      �� $��去除边框��6   $	��100��6$   $��30��60   $��0,0,10,0q�5*   ?   ��6   �� *MakeBorderlessCommand+6?   �� 5+      $��设置��6   $��80��6"   $��30��6-   ��5,      �� *OpenSettingsCommand+5-      �� 50   
   $��5��6   $ 
Horizontal=�6"   $	��Top=�6;   $��0,10,0,0q�6S   .+�51      �� -   5         ��Name$ chkFullscreen��51      $��全屏��6.   $��0,0,20,0q�6;   $��Center=�6M   �� 52      -   5        $��恢复窗口��52      $	��100��6$   60   53      55      56      