;; invoke: convert
;; expect: (f64:42)

(module
    (func (export "convert") (result f64)
        i32.const 42
        f64.convert_i32_u
    )
)