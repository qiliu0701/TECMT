from scipy.signal import find_peaks
from scipy.interpolate import interp1d
import numpy as np
import math
def svd(source):
    lengthsq = math.floor(math.sqrt(len(source)))
    length = lengthsq * lengthsq
    source=source[0:length ,:]
    for i in range(0,source.shape[1]):
        U, s, V = np.linalg.svd(source[:,i].reshape([int(lengthsq), -1]), full_matrices=False)
        selecter = np.zeros(int(lengthsq))
        selecter[0] = s[0]#只使用第一个奇异值分量
        svd_out = ((U[:, 0].reshape((-1, 1)) @ selecter[0].reshape((1, 1))) @ V[0, :].reshape((1, -1))).reshape(length)
        baoluo_out=baoluo(svd_out)
        if (i == 0):
            target = baoluo_out
        else:
            target = np.vstack((target, baoluo_out))
    return target.transpose()
def baoluo(source):
    length=source.shape[0]
    fft_result = np.fft.fft(source)
    filter = np.ones_like(fft_result)
    blank = math.floor(length / 180)
    filter[blank:len(filter) - blank] = 0
    fft_result = fft_result * filter
    filtered_signal = np.fft.ifft(fft_result)
    peaks, _ = find_peaks(np.real(filtered_signal))
    peaks_low, _ = find_peaks(-np.real(filtered_signal))
    up = np.real(filtered_signal)[peaks]
    down = np.real(filtered_signal)[peaks_low]
    peaks = np.append(np.append(0, peaks), length - 1)
    peaks_low = np.append(np.append(0, peaks_low), length - 1)
    up = np.append(np.append(np.real(filtered_signal)[0], up), np.real(filtered_signal)[-1])
    down = np.append(np.append(np.real(filtered_signal)[0], down), np.real(filtered_signal)[-1])
    interp_func_up = interp1d(peaks, up, kind='linear')
    interp_func_down = interp1d(peaks_low, down, kind='linear')
    peaks_interp = np.linspace(peaks[0], peaks[-1], num=length - 1)
    up_interp = interp_func_up(peaks_interp)
    peaks_low_interp = np.linspace(peaks_low[0], peaks_low[-1], num=length - 1)
    down_interp = interp_func_down(peaks_low_interp)
    mid_interp = (up_interp + down_interp) / 2
    return mid_interp