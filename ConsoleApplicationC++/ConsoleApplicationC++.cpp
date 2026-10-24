#include <iostream>     // Стандартне введення/виведення
#include <vector>       // Динамічні масиви
#include <iomanip>      // Маніпулятори форматування виводу
#include <windows.h>    // Налаштування кодування консолі Windows
#include <chrono>       // Вимірювання часу виконання

using namespace std;

// Перевірка кандидата за теоремою Корсельта
inline bool is_carmichael(long long n, const vector<int>& primes) {
    long long temp = n;
    int count = 0;

    for (int p : primes) {
        if ((long long)p * p > temp) break;
        if (temp % p == 0) {
            if ((temp / p) % p == 0) return false; // Не вільне від квадратів
            if ((n - 1) % (p - 1) != 0) return false; // (p-1) не ділить (n-1)
            count++;
            temp /= p;
        }
    }

    if (temp > 1) {
        if ((n - 1) % (temp - 1) != 0) return false;
        count++;
    }

    return count >= 3; // Число Кармайкла має щонайменше 3 простих дільники
}

int main(int argc, char** argv) {
    //  Налаштування консолі на UTF-8 
    SetConsoleCP(65001);
    SetConsoleOutputCP(65001);

    //  Прискорюємо введення/виведення 
    ios::sync_with_stdio(false);
    cin.tie(nullptr);

    // Встановіть N (наприклад, 1 000 000 000 для часу ~60 секунд)
    long long N = 100000000;   // Верхня межа пошуку
    const int RUNS = 3;         // Кількість повторних запусків
    double total_time = 0.0;    // Накопичувач часу

    // 0. Генерація простих чисел для швидкої перевірки дільників
    cout << "Підготовка: генерація решета простих чисел...\n";
    int limit = 32000; // sqrt(1 000 000 000) ≈ 31622
    vector<bool> is_prime(limit + 1, true);
    vector<int> primes;
    for (int p = 2; p <= limit; ++p) {
        if (is_prime[p]) {
            primes.push_back(p);
            for (int m = p * p; m <= limit; m += p) {
                is_prime[m] = false;
            }
        }
    }

    cout << "Послідовний пошук чисел Кармайкла для діапазону 1-" << N << "\n";

    //  Цикл тестових запусків 
    for (int run = 1; run <= RUNS; ++run) {
        // 1. Засікаємо час початку обчислень
        auto t0 = chrono::high_resolution_clock::now();

        long long count = 0;
        long long max_carmichael = 0;

        // 2. Оптимізований перебір
        for (long long i = 561; i <= N; i += 2) {
            // Швидкі перевірки перед важкою факторизацією:
            // Числа Кармайкла закінчуються лише на 1, 3, 7, 9 (не діляться на 5)
            long long last_digit = i % 10;
            if (last_digit == 5) continue;

            // Повна перевірка умов Корсельта
            if (is_carmichael(i, primes)) {
                ++count;
                max_carmichael = i;
            }
        }

        // 3. Засікаємо час завершення обчислень
        auto t1 = chrono::high_resolution_clock::now();
        chrono::duration<double> elapsed = t1 - t0;
        total_time += elapsed.count();

        // 4. Вивід результатів поточного запуску
        cout << "Запуск " << run
            << ": знайдено чисел Кармайкла = " << count
            << ", максимальне = " << max_carmichael
            << ", час = " << fixed << setprecision(6)
            << elapsed.count() << " секунд\n";
    }

    // 5. Обчислення та вивід середнього часу
    cout << "Середній час виконання: "
        << fixed << setprecision(6) << total_time / RUNS << " секунд\n";

    return 0;
}
