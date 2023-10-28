;; invoke: reinterpret
;; expect: (i64:4631107791820423168)

(module
    (memory 1)
    (func (export "reinterpret") (result i64)
        f64.const 42.0
        i64.reinterpret_f64
    )
)