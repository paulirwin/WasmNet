;; invoke: reinterpret
;; expect: (f64:42)

(module
    (memory 1)
    (func (export "reinterpret") (result f64)
        i64.const 4631107791820423168
        f64.reinterpret_i64
    )
)