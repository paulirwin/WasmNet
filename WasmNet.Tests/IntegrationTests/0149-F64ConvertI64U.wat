;; invoke: convert
;; expect: (f64:42000000000)

(module
    (func (export "convert") (result f64)
        i64.const 42000000000
        f64.convert_i64_u
    )
)